using System.Runtime.InteropServices;
using Swed64;

namespace KittyMenu
{
    // Auto bunny-hop: while space is held, re-trigger jump every time we land.
    // 65537 = +jump pressed, 256 = +jump released (CS2 input bitmasks).
    public static class Bhop
    {
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        const int VK_SPACE = 0x20;
        const int FL_ONGROUND = 1; // bit 0 of m_fFlags

        static bool wasPressed = false;

        public static void Run(Swed swed, IntPtr client, Entity local)
        {
            if (local.PawnAddress == IntPtr.Zero)
                return;

            bool spaceHeld = (GetAsyncKeyState(VK_SPACE) & 0x8000) != 0;

            if (!spaceHeld)
            {
                // let go of jump so the user's manual movement still works
                if (wasPressed)
                {
                    swed.WriteInt(client + Offsets.Jump, 256);
                    wasPressed = false;
                }
                return;
            }

            int flags = swed.ReadInt(local.PawnAddress, Offsets.MFlags);
            if ((flags & FL_ONGROUND) != 0)
            {
                swed.WriteInt(client + Offsets.Jump, 65537); // +jump
                wasPressed = true;
            }
            else
            {
                swed.WriteInt(client + Offsets.Jump, 256);   // -jump while airborne
                wasPressed = false;
            }
        }
    }
}
