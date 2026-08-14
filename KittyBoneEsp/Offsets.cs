namespace KittyMenu
{
    // Offsets for the current CS2 build. Grouped by the target structure/pattern
    // so the game-breaking changes are easy to spot and update together.
    public static class Offsets
    {
        // client.dll global offsets (offsets.cs)
        public const int DwEntityList = 0x2554050;
        public const int DwLocalPlayerPawn = 0x23A9118;
        public const int DwViewMatrix = 0x23AE550;
        public const int DwViewAngles = 0x23BF1A8;

        // CCSGameRules (round-end / MVP detection)
        public const int DwGameRules = 0x23A8BD8; // C_CSGameRules global in client.dll
        public const int MRoundEndReason = 0xF0C; // C_CSGameRules.m_eRoundEndReason
        public const int MRoundEndCount = 0xF44;  // C_CSGameRules.m_nRoundEndCount (+1 per ended round)

        // CEntityList (paged array)
        public const int ChunkEntryOffset = 0x10; // listEntry at entityList + 8*chunk + 0x10
        public const int ListEntryStride = 0x70;  // per-entity stride inside a chunk
        public const int DwGameEntitySystemHighestIndex = 0x2090; // highest used entity index

        // CBaseEntity / C_BasePlayerPawn
        public const int MTeamNum = 0x3E7;
        public const int MHealth = 0x34C;              // m_iHealth (int)
        public const int MLifeState = 0x354;
        public const int MVOldOrigin = 0x13B8;
        public const int MFlags = 0x3F4;              // FL_ONGROUND = 1
        public const int MPGameSceneNode = 0x330;
        public const int MModelState = 0x140;         // dwBoneMatrix lives at +0x80
        public const int MAimPunchServices = 0x14B8;  // C_CSPlayerPawn.m_pAimPunchServices
        public const int MWeaponServices = 0x1208;
        public const int MCameraServices = 0x1240;
        public const int MObserverServices = 0x1220;

        // CBasePlayerController
        public const int MHPlayerPawn = 0x914;
        public const int MPlayerName = 0x6F4;          // m_iszPlayerName (inline char[128])
        public const int MSanitizedName = 0x868;       // m_sSanitizedPlayerName (CUtlString: a pointer, not inline)

        // C_BaseModelEntity
        public const int MGlow = 0xDE0;              // m_Glow (inline CGlowProperty)

        // C_BasePlayerPawn
        public const int MServerViewAngleChanges = 0x1258; // C_UtlVectorEmbeddedNetworkVar<ViewAngleServerChange_t>
        public const int MVAngle = 0x12C0;               // v_angle (current view QAngle)

        // C_CSPlayerPawnBase
        public const int MIDEntIndex = 0x342C;           // entity under the crosshair
        public const int MBIsScoped = 0x1C78;

        // visibility (spotted-by-mask, the external wall-check)
        public const int MEntitySpottedState = 0x1C60;  // C_CSPlayerPawn.m_entitySpottedState
        public const int MSpottedByMask = 0xC;          // EntitySpottedState_t.m_bSpottedByMask (uint32[2])
        public const int MBIsLocalPlayerController = 0x788; // CCSPlayerController.m_bIsLocalPlayerController

        // CPlayer_WeaponServices / C_CSWeaponBase
        public const int MActiveWeapon = 0x60;         // m_hActiveWeapon (handle)
        public const int MAccuracyPenalty = 0x17F0;    // movement spread
        public const int MRecoilIndex = 0x1800;        // m_flRecoilIndex (spray spread)

        // CCSPlayerBase_CameraServices
        public const int MFov = 0x290;                 // current, animated
        public const int MFovStart = 0x294;            // target
        public const int MFovRate = 0x29C;             // transition speed

        // CPlayer_ObserverServices
        public const int MObserverMode = 0x48;         // uint8, 3 = chase/thirdperson
        public const int MObserverForcedMode = 0x54;   // m_bForcedObserverMode (bool)
        public const int MObserverChaseDistance = 0x58; // m_flObserverChaseDistance (float)

        // C_CSPlayerPawnBase flash fields
        public const int MFlashBangTime = 0x1414;
        public const int MFlashScreenshotAlpha = 0x1418;
        public const int MFlashOverlayAlpha = 0x141C;
        public const int MFlashBuildUp = 0x1420;
        public const int MFlashMaxAlpha = 0x1424;
        public const int MFlashDuration = 0x1428;

        // C_EnvSky (map sky entity)
        public const int EnvSkyMEnabled = 0xFE4;    // m_bEnabled (bool): false hides the sky

        // input buttons (client.dll + offset, buttons.json)
        public const int Jump = 0x2099510;
        public const int Attack = 0x2099000;
    }
}
