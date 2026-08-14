using System.Collections.Generic;
using System.Numerics;

namespace KittyMenu
{
    // A player entity snapshot for the current frame. Bone data is keyed by
    // BoneIds so callers never have to remember magic indices.
    public class Entity
    {
        public IntPtr PawnAddress { get; set; }
        public IntPtr ControllerAddress { get; set; }
        public Vector3 Origin { get; set; }
        public int Team { get; set; }
        public int Health { get; set; }
        public string Name { get; set; } = "";
        public uint LifeState { get; set; }
        public float Distance { get; set; }
        public bool Spotted { get; set; } // visible to the local player (spotted mask)

        public Dictionary<BoneIds, Vector3> Bones { get; set; } = new();
        public Dictionary<BoneIds, Vector2> Bones2d { get; set; } = new();

        public bool IsAlive => Health > 0 && LifeState >= 256; // health 0 or a stale lifeState both mean dead

        public bool IsEnemy(Entity local) => Team != local.Team;

        public Vector3 Bone(BoneIds bone) =>
            Bones.TryGetValue(bone, out var v) ? v : Vector3.Zero;

        public Vector2 Bone2d(BoneIds bone) =>
            Bones2d.TryGetValue(bone, out var v) ? v : new Vector2(-1, -1);
    }

    // Named skeleton bones -> their indices in the bone matrix.
    public enum BoneIds
    {
        Waist = 1,       // 0 (pelvis/spine base)
        Neck = 5,        // 1
        Head = 6,        // 2
        ShoulderLeft = 8,   // 3
        ForeLeft = 9,       // 4
        HandLeft = 11,      // 5
        ShoulderRight = 12, // 6
        ForeRight = 13,     // 7
        HandRight = 15,     // 8
        KneeLeft = 18,      // 9
        FeetLeft = 19,      // 10
        KneeRight = 21,     // 11
        FeetRight = 22      // 12
    }
}
