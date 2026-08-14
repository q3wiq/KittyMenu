using System.Numerics;
using Swed64;

namespace KittyMenu
{
    // Runs every enabled feature once per frame. The main loop just feeds it
    // the current frame data; this keeps Program.cs free of per-feature logic.
    public static class Features
    {
        public static void Run(Swed swed, IntPtr client, IntPtr entityList,
            Settings s, Entity local, List<Entity> entities, Vector2 screen)
        {
            // no-recoil first so aim punch is cancelled before the aimbot snaps
            if (s.NoRecoil)
                NoRecoil.Run(swed, client, entityList, local, s.RecoilStrength);

            if (s.Bhop)
                Bhop.Run(swed, client, local);

            // troll: random jump sound whenever the player leaves the ground.
            // troll mode keeps it on even if the checkbox was toggled off
            bool troll = s.Mode == Settings.MenuMode.Troll;
            if (s.JumpSounds || troll)
                JumpSounds.Run(swed, client, local);

            // troll: custom round-end / MVP sound when a round finishes
            if (s.RoundEndSound || troll)
                RoundEndSounds.Run(swed, client);

            if (s.AntiFlash)
                AntiFlash.Run(swed, local);

            if (s.CameraFov)
                CameraFov.Run(swed, local, s.CameraFovValue);

            // glow runs every frame (not just when enabled) so it can clean up
            // when the toggle is turned off
            Glow.Run(swed, entities, local, s.Glow, s.TeamColor, s.EnemyColor);

            if (s.Aimbot)
                Aim.Run(swed, client, entities, local, s.AimFov, screen, s.AimSmooth, s.AimTarget, s.Deathmatch);

            // silent aim runs after the aimbot so it wins while firing; it only
            // writes for the single tick the shot goes out, so the two don't fight
            if (s.SilentAim)
                SilentAim.Run(swed, client, entities, local, s.AimFov, screen, s.AimTarget, s.Deathmatch);

            // triggerbot runs last and only while enabled so it never overrides
            // normal mouse input (holding to shoot, charging nades)
            if (s.TriggerBot)
                TriggerBot.Run(swed, client, entityList, local, s.TriggerKey, s.Deathmatch);
        }
    }
}
