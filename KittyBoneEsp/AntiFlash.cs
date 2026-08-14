using Swed64;

namespace KittyMenu
{
    // Anti-flash: zeroes every flash-related field on the local pawn so a
    // flashbang never whitens the screen. Written each frame so the engine's
    // flash timer can't build back up.
    public static class AntiFlash
    {
        public static void Run(Swed swed, Entity local)
        {
            if (local.PawnAddress == IntPtr.Zero)
                return;

            IntPtr pawn = local.PawnAddress;
            swed.WriteFloat(pawn, Offsets.MFlashMaxAlpha, 0f);
            swed.WriteFloat(pawn, Offsets.MFlashDuration, 0f);
            swed.WriteFloat(pawn, Offsets.MFlashBangTime, 0f);
            swed.WriteFloat(pawn, Offsets.MFlashOverlayAlpha, 0f);
            swed.WriteFloat(pawn, Offsets.MFlashScreenshotAlpha, 0f);
            swed.WriteBool(pawn, Offsets.MFlashBuildUp, false);
        }
    }
}
