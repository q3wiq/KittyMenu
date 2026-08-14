using System.Text;
using Swed64;
using System.Numerics;

namespace KittyMenu
{
    // Reads the view matrix, bones, and the player entity list once per frame.
    public class Reader
    {
        private readonly Swed _swed;
        private readonly IntPtr _client;
        private readonly Vector2 _screenSize;

        public Reader(Swed swed, IntPtr client, Vector2 screenSize)
        {
            _swed = swed;
            _client = client;
            _screenSize = screenSize;
        }

        public Matrix4x4 ReadViewMatrix()
        {
            var m = _swed.ReadMatrix(_client + Offsets.DwViewMatrix);
            return new Matrix4x4(
                m[0], m[1], m[2], m[3],
                m[4], m[5], m[6], m[7],
                m[8], m[9], m[10], m[11],
                m[12], m[13], m[14], m[15]);
        }

        // Reads every BoneIds bone from the bone matrix (27 bones * 32-byte stride).
        public Dictionary<BoneIds, Vector3> ReadBones(IntPtr boneMatrix)
        {
            byte[] bytes = _swed.ReadBytes(boneMatrix, 27 * 32 + 16);
            var bones = new Dictionary<BoneIds, Vector3>();

            foreach (BoneIds bone in Enum.GetValues<BoneIds>())
            {
                int offset = (int)bone * 32;
                bones[bone] = new Vector3(
                    BitConverter.ToSingle(bytes, offset + 0),
                    BitConverter.ToSingle(bytes, offset + 4),
                    BitConverter.ToSingle(bytes, offset + 8));
            }
            return bones;
        }

        public Dictionary<BoneIds, Vector2> ProjectBones(Dictionary<BoneIds, Vector3> bones, Matrix4x4 viewMatrix)
        {
            var projected = new Dictionary<BoneIds, Vector2>(bones.Count);
            foreach (var (bone, world) in bones)
                projected[bone] = Calculate.WorldToScreen(viewMatrix, world, _screenSize);
            return projected;
        }

        // Returns the local player's address from the client globals.
        public IntPtr GetLocalPlayerPawn() => _swed.ReadPointer(_client, Offsets.DwLocalPlayerPawn);

        // True when the entity's spotted mask has the local player's bit set:
        // the game marks an enemy visible when it's in the local player's line
        // of sight, and unmarks it when walls block the view.
        private bool IsSpottedBy(IntPtr pawn, int localIndex)
        {
            if (pawn == IntPtr.Zero)
                return false;

            byte[] bytes = _swed.ReadBytes(pawn, Offsets.MEntitySpottedState + Offsets.MSpottedByMask, 8);
            uint mask = localIndex <= 32
                ? BitConverter.ToUInt32(bytes, 0)
                : BitConverter.ToUInt32(bytes, 4);

            return (mask & (1u << ((localIndex - 1) % 32))) != 0;
        }

        // player name is an inline char[128]; decode UTF-8 up to the first null
        private string ReadName(IntPtr controller)
        {
            byte[] bytes = _swed.ReadBytes(controller, Offsets.MPlayerName, 128);
            int len = 0;
            while (len < bytes.Length && bytes[len] != 0)
                len++;
            return Encoding.UTF8.GetString(bytes, 0, len);
        }

        // Iterates the player list, resolves each controller + pawn, and reads
        // the data needed for ESP. Skips ourself and any dead player.
        public List<Entity> ReadEntities(IntPtr entityList, Entity local, Matrix4x4 viewMatrix)
        {
            var entities = new List<Entity>();

            // the spotted-mask bits are indexed by the local player's entity
            // slot, so resolve it up front (cheap: 1 bool per occupied slot)
            int localIndex = 1;
            for (int i = 1; i <= 64; i++)
            {
                IntPtr controller = EntityList.GetEntry(_swed, entityList, i);
                if (controller == IntPtr.Zero)
                    continue;
                if (_swed.ReadBytes(controller, Offsets.MBIsLocalPlayerController, 1)[0] != 0)
                {
                    localIndex = i;
                    break;
                }
            }

            for (int i = 0; i < 64; i++)
            {
                IntPtr controller = EntityList.GetEntry(_swed, entityList, i);
                if (controller == IntPtr.Zero)
                    continue;

                int pawnHandle = _swed.ReadInt(controller, Offsets.MHPlayerPawn);
                if (pawnHandle <= 0)
                    continue;

                IntPtr pawn = EntityList.GetEntry(_swed, entityList, pawnHandle);
                if (pawn == IntPtr.Zero || pawn == local.PawnAddress)
                    continue;

                uint lifeState = _swed.ReadUInt(pawn, Offsets.MLifeState);
                int health = _swed.ReadInt(pawn, Offsets.MHealth);
                // lifeState lags for a few frames after a kill; health is the
                // instant signal, so drop anyone that's dead by either check
                if (lifeState < 256 || health <= 0)
                    continue;

                var entity = new Entity
                {
                    PawnAddress = pawn,
                    ControllerAddress = controller,
                    Team = _swed.ReadInt(pawn, Offsets.MTeamNum),
                    Health = health,
                    Name = ReadName(controller),
                    LifeState = lifeState,
                    Origin = _swed.ReadVec(pawn, Offsets.MVOldOrigin),
                    Distance = Vector3.Distance(local.Origin, _swed.ReadVec(pawn, Offsets.MVOldOrigin)),
                    Spotted = IsSpottedBy(pawn, localIndex),
                };

                IntPtr sceneNode = _swed.ReadPointer(pawn, Offsets.MPGameSceneNode);
                IntPtr boneMatrix = _swed.ReadPointer(sceneNode, Offsets.MModelState + 0x80); // dwBoneMatrix
                entity.Bones = ReadBones(boneMatrix);
                entity.Bones2d = ProjectBones(entity.Bones, viewMatrix);

                entities.Add(entity);
            }

            return entities;
        }
    }
}
