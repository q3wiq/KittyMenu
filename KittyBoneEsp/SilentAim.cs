using System.Numerics;
using System.Runtime.InteropServices;
using Swed64;

namespace KittyMenu
{
    // Silent aim: redirects your shot onto a target without moving your
    // crosshair. On the frame you press fire we write the aim angle to the
    // view angles and HOLD it just long enough for one command tick to pick
    // it up (~15ms), then snap back to your real aim. Your screen barely
    // moves, the server simulates the shot at the target, and other players
    // see a quick drag on the model instead of a snap.
    //
    // Target acquisition is ANGULAR (degrees from the current view angle),
    // not a pixel circle: a target 200m out no longer has to sit dead-centre
    // in a tiny pixel radius, so silent aim works at every range the same way.
    public static class SilentAim
    {
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        const int MouseLeft = 0x01;
        const float Rad2Deg = 57.2957795130823209f;
        const int HoldFrames = 5; // ~15ms at the 3ms loop -> one 64-tick server frame

        static bool _armed;
        static Vector3 _aim;
        static Vector3 _prevView;
        static int _holdFrames;

        public static void Run(Swed swed, IntPtr client, List<Entity> entities,
            Entity local, float fovPx, Vector2 screen, Settings.AimPart aimTarget,
            bool deathmatch)
        {
            if (local == null || local.PawnAddress == IntPtr.Zero)
            {
                _armed = false;
                return;
            }

            Vector3 curView = swed.ReadVec(client + Offsets.DwViewAngles, 0);

            // while armed we are inside the shot window: keep the angle, then
            // restore the user's real aim once the tick has passed
            if (_armed)
            {
                if (--_holdFrames <= 0 || (GetAsyncKeyState(MouseLeft) & 0x8000) == 0)
                {
                    _armed = false;
                    swed.WriteVec(client + Offsets.DwViewAngles, _prevView);
                    swed.WriteVec(local.PawnAddress, Offsets.MVAngle, _prevView);
                }
                else
                {
                    swed.WriteVec(client + Offsets.DwViewAngles, _aim);
                    swed.WriteVec(local.PawnAddress, Offsets.MVAngle, _aim);
                }
                return;
            }

            // arm only on the click (rising edge), not while already holding
            if ((GetAsyncKeyState(MouseLeft) & 0x8000) == 0)
                return;

            if (!FindTarget(entities, local, screen, fovPx, aimTarget, curView, deathmatch,
                    out Vector3 angle))
                return;

            _aim = angle;
            _prevView = curView;
            _armed = true;
            _holdFrames = HoldFrames;

            swed.WriteVec(client + Offsets.DwViewAngles, _aim);
            swed.WriteVec(local.PawnAddress, Offsets.MVAngle, _aim);
        }

        // picks the enemy whose required aim angle is closest to the current
        // view, inside an angular fov that scales with the pixel slider.
        static bool FindTarget(List<Entity> entities, Entity local, Vector2 screen,
            float fovPx, Settings.AimPart aimTarget, Vector3 view, bool deathmatch,
            out Vector3 angle)
        {
            angle = default;

            // map the pixel-radius slider to degrees via the vertical fov
            // (screen half-height holds ~45 deg at 90 fov), making the window
            // range-independent
            float capDeg = fovPx * 45f / (screen.Y / 2f);

            BoneIds[] parts = { BoneIds.Head, BoneIds.Neck, BoneIds.Waist };
            Vector3 eye = local.Origin + new Vector3(0, 0, 64);

            Entity? best = null;
            float bestAng = capDeg;

            foreach (var e in entities)
            {
                if (!e.IsAlive || (!deathmatch && !e.IsEnemy(local)) || !e.Spotted)
                    continue;

                int start = aimTarget == Settings.AimPart.Auto ? 0 : (int)aimTarget;
                int count = aimTarget == Settings.AimPart.Auto ? parts.Length : 1;

                for (int i = start; i < start + count; i++)
                {
                    Vector3? a = CalcAngle(eye, e.Bone(parts[i]));
                    if (a == null)
                        continue;

                    float d = AngularDistance(view, a.Value);
                    if (d <= bestAng)
                    {
                        bestAng = d;
                        best = e;
                        angle = a.Value;
                    }
                }
            }
            return best != null;
        }

        static float AngularDistance(Vector3 view, Vector3 target)
        {
            float pitchDiff = MathF.Abs(target.X - view.X);
            float yawDiff = ((target.Y - view.Y + 540f) % 360f) - 180f;
            return MathF.Sqrt(pitchDiff * pitchDiff + yawDiff * yawDiff);
        }

        static Vector3? CalcAngle(Vector3 eye, Vector3 target)
        {
            Vector3 delta = eye - target; // same convention as the aimbot
            float hyp = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
            if (hyp < 0.001f)
                return null;

            float pitch = MathF.Atan(delta.Z / hyp) * Rad2Deg;
            float yaw = MathF.Atan(delta.Y / delta.X) * Rad2Deg;
            if (delta.X >= 0)
                yaw += 180f;
            return new Vector3(pitch, yaw, 0);
        }
    }
}