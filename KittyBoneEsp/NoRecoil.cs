using Swed64;
using System.Numerics;

namespace KittyMenu
{
    // No-recoil (recoil control) + no movement/recoil spread for the current CS2 schema.
    // recoil: aim punch lives in CCSPlayer_AimPunchServices:
    //   pawn -> m_pAimPunchServices -> m_aimPunchCache (CUtlVector of QAngles,
    //   12 bytes/element, last element = current punch)
    // spread: m_fAccuracyPenalty and m_flRecoilIndex on the active weapon are
    // pinned to zero every frame so running/strafing/spraying never grows the
    // shot cone.
    //
    // Instead of compensating the kick (which can overshoot and drag the view),
    // we remove the punch AT THE SOURCE: the predictable/unpredictable base
    // angles are zeroed and the punch cache is cleared every frame, so the game
    // never applies recoil to the view at all. The crosshair stays exactly
    // where you aim and your mouse movement is never touched (we don't write
    // view angles at all).
    public static class NoRecoil
    {
        const int CacheCountOffset = 0x88; // CCSPlayer_AimPunchServices::m_unpredictableBaseTick - 0x18
        const int CacheDataOffset = 0x90;  // +8: the Data pointer of the CUtlVector

        const int PredictableBaseAngle = 0x50;   // QAngle (12 bytes)
        const int UnpredictableBaseAngle = 0xA4; // QAngle (12 bytes)

        public static void Run(Swed swed, IntPtr client, IntPtr entityList, Entity local, float strength)
        {
            if (local.PawnAddress == IntPtr.Zero)
                return;

            // kill movement/recoil spread on the held weapon (runs even when
            // idle so strafing never opens up your shot cone)
            NoMovementSpread(swed, entityList, local.PawnAddress);

            if (strength <= 0.001f)
                return;

            IntPtr services = swed.ReadPointer(local.PawnAddress, Offsets.MAimPunchServices);
            if (services == IntPtr.Zero)
                return;

            // zero the base angles the recoil spring uses as input
            swed.WriteBytes(services, PredictableBaseAngle, new byte[12]);
            swed.WriteBytes(services, UnpredictableBaseAngle, new byte[12]);

            // clear the punch cache so no kick is queued up, and reset its
            // length so the game reads an empty vector
            int count = swed.ReadInt(services, CacheCountOffset);
            if (count > 0 && count <= 1000)
            {
                IntPtr data = swed.ReadPointer(services, CacheDataOffset);
                if (data != IntPtr.Zero)
                    swed.WriteBytes(data, 0, new byte[count * 12]);
                swed.WriteInt(services, CacheCountOffset, 0);
            }
        }

        // keep the shot cone pinned shut: zero both the movement/spray spread
        // fields on the active weapon. the game recalculates them every frame,
        // so we hammer them continuously.
        static void NoMovementSpread(Swed swed, IntPtr entityList, IntPtr localPawn)
        {
            if (entityList == IntPtr.Zero)
                return;

            IntPtr weaponServices = swed.ReadPointer(localPawn, Offsets.MWeaponServices);
            if (weaponServices == IntPtr.Zero)
                return;

            int weaponHandle = swed.ReadInt(weaponServices, Offsets.MActiveWeapon);
            if (weaponHandle <= 0)
                return;

            IntPtr weapon = EntityList.GetEntry(swed, entityList, weaponHandle);
            if (weapon == IntPtr.Zero)
                return;

            swed.WriteFloat(weapon, Offsets.MAccuracyPenalty, 0f);
            swed.WriteFloat(weapon, Offsets.MRecoilIndex, 0f);
        }
    }
}