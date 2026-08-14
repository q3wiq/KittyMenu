using Swed64;

namespace KittyMenu
{
    // Camera FOV changer. Pins the local camera's FOV on the camera services.
    // The engine lerps m_iFOV toward m_iFOVStart during reloads/weapon switches -
    // that's the flicker - so m_flFOVRate is pinned near zero to make the
    // transition instant and correct the reset within the same frame.
    public static class CameraFov
    {
        public static void Run(Swed swed, Entity local, float value)
        {
            if (local.PawnAddress == IntPtr.Zero)
                return;

            IntPtr cameraServices = swed.ReadPointer(local.PawnAddress, Offsets.MCameraServices);
            if (cameraServices == IntPtr.Zero)
                return;

            if (swed.ReadInt(local.PawnAddress, Offsets.MBIsScoped) != 0)
                return; // leave scoped weapons alone

            int target = (int)value;
            if (swed.ReadInt(cameraServices, Offsets.MFov) != target)
                swed.WriteInt(cameraServices, Offsets.MFov, target);
            if (swed.ReadInt(cameraServices, Offsets.MFovStart) != target)
                swed.WriteInt(cameraServices, Offsets.MFovStart, target);
            swed.WriteFloat(cameraServices, Offsets.MFovRate, 0.000005f); // instant transitions
        }
    }
}
