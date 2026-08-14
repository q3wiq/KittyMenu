using Swed64;
using System.Numerics;

namespace KittyMenu
{
    public static class Aim
    {
        const float Rad2Deg = 57.2957795130823209f;

        // Smooth-snaps at the enemy whose on-screen aim bone is closest to the
        // center of the screen AND inside the fov circle (radius in pixels).
        // smooth: 0 = instant, 1 = extremely slow/stealthy.
        // aimTarget: which body part to lock onto (Auto = nearest of the three).
        public static void Run(Swed swed, IntPtr client, List<Entity> entities,
            Entity local, float fovPx, Vector2 screen, float smooth, Settings.AimPart aimTarget,
            bool deathmatch)
        {
            Vector2 center = new Vector2(screen.X / 2, screen.Y / 2);

            BoneIds[] parts = { BoneIds.Head, BoneIds.Neck, BoneIds.Waist };
            Entity best = null;
            BoneIds bestBone = BoneIds.Head;
            float bestDist = fovPx;

            foreach (var e in entities)
            {
                // only lock eligible targets: enemies (or everyone in FFA) the
                // local player can actually see
                if (!e.IsAlive || (!deathmatch && !e.IsEnemy(local)) || !e.Spotted)
                    continue;

                // a fixed body part, or every part when set to Auto
                int start = aimTarget == Settings.AimPart.Auto ? 0 : (int)aimTarget;
                int count = aimTarget == Settings.AimPart.Auto ? parts.Length : 1;

                for (int i = start; i < start + count; i++)
                {
                    BoneIds bone = parts[i];
                    Vector2 p = e.Bone2d(bone);
                    if (p.X < 0 || p.Y < 0) // behind camera / invalid
                        continue;

                    float dist = Vector2.Distance(p, center);
                    if (dist <= bestDist)
                    {
                        bestDist = dist;
                        best = e;
                        bestBone = bone;
                    }
                }
            }

            if (best == null)
                return;

            // eye position is the pawn origin + eye height
            Vector3 eye = local.Origin + new Vector3(0, 0, 64);
            Vector3 target = best.Bone(bestBone);
            Vector3 delta = eye - target; // proven source-engine CalcAngle convention

            float hyp = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
            if (hyp < 0.001f)
                return;

            float pitch = MathF.Atan(delta.Z / hyp) * Rad2Deg;
            float yaw = MathF.Atan(delta.Y / delta.X) * Rad2Deg;
            if (delta.X >= 0)
                yaw += 180f;

            // read current view angles and step toward the target instead of snapping
            Vector3 view = swed.ReadVec(client + Offsets.DwViewAngles, 0);

            float ease = 1f - Math.Clamp(smooth, 0f, 1f); // 1 = instant, 0 = frozen

            // pitch: straight lerp, clamped to a legal range
            float newPitch = view.X + (pitch - view.X) * ease;
            newPitch = Math.Clamp(newPitch, -89f, 89f);

            // yaw: shortest-way-around wrapping then lerp
            float diff = ((yaw - view.Y + 540f) % 360f) - 180f;
            float newYaw = view.Y + diff * ease;

            if (ease < 1f && Math.Abs(newPitch - pitch) < 0.01f
                && Math.Abs(newYaw - yaw) < 0.01f)
                return; // already on target, stop burning writes

            swed.WriteVec(client + Offsets.DwViewAngles, new Vector3(newPitch, newYaw, 0));
        }
    }
}
