using System.Numerics;

namespace KittyMenu
{
    // World -> screen projection for the current view matrix.
    public static class Calculate
    {
        // Returns screen coordinates, or (-1, -1) if the point is behind the camera.
        public static Vector2 WorldToScreen(Matrix4x4 matrix, Vector3 pos, Vector2 screenSize)
        {
            // perspective w-component of the point in view space
            float screenW = matrix.M41 * pos.X + matrix.M42 * pos.Y + matrix.M43 * pos.Z + matrix.M44;

            if (screenW <= 0.001f)
                return new Vector2(-1, -1); // behind the camera

            float screenX = matrix.M11 * pos.X + matrix.M12 * pos.Y + matrix.M13 * pos.Z + matrix.M14;
            float screenY = matrix.M21 * pos.X + matrix.M22 * pos.Y + matrix.M23 * pos.Z + matrix.M24;

            float camX = screenSize.X / 2f;
            float camY = screenSize.Y / 2f;

            // perspective divide + center on the screen (Y is flipped)
            return new Vector2(
                camX + camX * screenX / screenW,
                camY - camY * screenY / screenW);
        }
    }
}
