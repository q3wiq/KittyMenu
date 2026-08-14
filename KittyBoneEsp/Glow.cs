using System.Numerics;
using Swed64;

namespace KittyMenu
{
    // Real in-engine glow (the "charm" effect): flips C_BaseModelEntity::m_Glow
    // on each player pawn so the engine itself renders a colored glow through
    // walls. Runs every frame and turns the glow back off for anyone we were
    // glowing who died/disconnected, or when the toggle is disabled.
    public static class Glow
    {
        // CGlowProperty sub-offsets (C_BaseModelEntity.m_Glow + X)
        const int GlowColorVec = 0x8;     // m_fGlowColor (Vector rgb)
        const int GlowType = 0x30;        // m_iGlowType, 3 = full glow
        const int GlowColor = 0x40;       // m_glowColorOverride (Color rgba)
        const int GlowBGlowing = 0x51;    // m_bGlowing

        static readonly HashSet<IntPtr> _glowing = new();

        public static void Run(Swed swed, List<Entity> entities, Entity local,
            bool enabled, Vector4 teamColor, Vector4 enemyColor)
        {
            if (enabled)
            {
                // keep everyone in the list glowing in their esp color
                foreach (Entity e in entities)
                {
                    if (e.PawnAddress == IntPtr.Zero)
                        continue;
                    Apply(swed, e.PawnAddress, e.IsEnemy(local) ? enemyColor : teamColor);
                    _glowing.Add(e.PawnAddress);
                }

                // un-glow anyone we set earlier who isn't in the list anymore
                if (_glowing.Count > entities.Count)
                {
                    var gone = new List<IntPtr>();
                    foreach (IntPtr p in _glowing)
                        if (!entities.Any(e => e.PawnAddress == p))
                            gone.Add(p);
                    foreach (IntPtr p in gone)
                    {
                        Clear(swed, p);
                        _glowing.Remove(p);
                    }
                }
            }
            else
            {
                foreach (IntPtr p in _glowing)
                    Clear(swed, p);
                _glowing.Clear();
            }
        }

        static void Apply(Swed swed, IntPtr pawn, Vector4 color)
        {
            IntPtr glow = pawn + Offsets.MGlow;

            // write both color fields; different engine paths read one or the other
            swed.WriteVec(glow, GlowColorVec, new Vector3(color.X, color.Y, color.Z));
            byte[] rgba =
            {
                (byte)(Math.Clamp(color.X, 0f, 1f) * 255),
                (byte)(Math.Clamp(color.Y, 0f, 1f) * 255),
                (byte)(Math.Clamp(color.Z, 0f, 1f) * 255),
                (byte)(Math.Clamp(color.W, 0f, 1f) * 255)
            };
            swed.WriteBytes(glow, GlowColor, rgba);

            swed.WriteInt(glow, GlowType, 3);
            swed.WriteBool(glow, GlowBGlowing, true);
        }

        static void Clear(Swed swed, IntPtr pawn)
        {
            IntPtr glow = pawn + Offsets.MGlow;
            swed.WriteBool(glow, GlowBGlowing, false);
        }
    }
}
