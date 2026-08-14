using System.Runtime.InteropServices;
using Swed64;

namespace KittyMenu
{
    // Triggerbot: while the trigger key is held, if an enemy is under the
    // crosshair (m_iIDEntIndex) and alive, hold the attack button. Releases
    // attack only if the triggerbot itself is the one holding it, so normal
    // mouse input (holding to shoot, charging nades) is never overridden.
    // attack = 65537 pressed, 256 released (same bitmask convention as jump).
    public static class TriggerBot
    {
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        static bool holding = false;

        public static void Run(Swed swed, IntPtr client, IntPtr entityList, Entity local, int hotkey,
            bool deathmatch)
        {
            // only act while the user is holding the trigger key
            bool keyHeld = (GetAsyncKeyState(hotkey) & 0x8000) != 0;

            if (local.PawnAddress == IntPtr.Zero || !keyHeld)
            {
                // release only if we were the ones holding attack
                if (holding)
                {
                    swed.WriteInt(client + Offsets.Attack, 256);
                    holding = false;
                }
                return;
            }

            int targetIndex = swed.ReadInt(local.PawnAddress, Offsets.MIDEntIndex);
            IntPtr target = IntPtr.Zero;
            if (targetIndex > 0)
                target = EntityList.GetEntry(swed, entityList, targetIndex);

            bool validTarget = target != IntPtr.Zero && target != local.PawnAddress;
            if (validTarget)
            {
                int team = swed.ReadInt(target, Offsets.MTeamNum);
                uint lifeState = swed.ReadUInt(target, Offsets.MLifeState);

                // enemies only (anyone in FFA), and alive players read
                // lifeState >= 256 on this build
                if ((deathmatch || team != local.Team) && lifeState >= 256)
                {
                    swed.WriteInt(client + Offsets.Attack, 65537);
                    holding = true;
                    return;
                }
            }

            // no valid target; release only if we were the ones holding it
            if (holding)
            {
                swed.WriteInt(client + Offsets.Attack, 256);
                holding = false;
            }
        }
    }
}
