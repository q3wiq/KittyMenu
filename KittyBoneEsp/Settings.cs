using System.IO;
using System.Numerics;
using System.Text.Json;

namespace KittyMenu
{
    // All user-configurable cheat settings + JSON persistence. Kept separate
    // from the renderer so the menu just binds to it.
    public class Settings
    {
        // visuals
        public bool Esp = true;
        public bool EspBox = true;
        public bool EspHealthBar = true;
        public bool EspTracer = true;
        public bool EspName = true;
        public bool EspSkeleton = true;
        public float BoneThickness = 4;
        public bool Glow = false;        // in-engine glow ("charm") wall-through highlight

        // aim
        public bool Aimbot = false;
        public bool SilentAim = false;
        public float AimSmooth = 0.85f; // 0 = instant, 1 = very slow
        public float AimFov = 300;      // radius of the aim fov circle in pixels
        public AimPart AimTarget = AimPart.Head;
        public bool Deathmatch = false; // treat everyone as hostile (FFA) regardless of team

        // misc
        public bool NoRecoil = false;
        public float RecoilStrength = 1.0f; // 0..1 factor applied to punch compensation
        public bool Bhop = false;
        public bool AntiFlash = false;
        public bool CameraFov = false;
        public float CameraFovValue = 90f;
        public bool TriggerBot = false;
        public int TriggerKey = 0x58; // VK_X, key that arms the triggerbot

        // mode + troll
        public MenuMode Mode = MenuMode.None;
        public bool JumpSounds = false;      // play a random sound on every jump
        public bool RoundEndSound = false;   // play a custom round-end / MVP sound

        // colors
        public Vector4 TeamColor = new Vector4(1, 1, 1, 1);
        public Vector4 EnemyColor = new Vector4(1, 1, 1, 1);

        // which body part the aimbot locks onto
        public enum AimPart
        {
            Head = 0,
            Neck = 1,
            Body = 2,   // waist/pelvis
            Auto = 3    // closest of the above to screen center
        }

        // which cheat preset the user picked in the Mode tab
        public enum MenuMode
        {
            None = 0,
            Rage = 1,
            Legit = 2,
            Troll = 3
        }

        // applies the preset settings for the selected mode. Individual feature
        // toggles stay flickable afterwards, this just sets up the defaults.
        public void ApplyMode(MenuMode mode)
        {
            Mode = mode;
            switch (mode)
            {
                case MenuMode.Rage:
                    Aimbot = true;
                    SilentAim = true;        // max: the shot itself is redirected
                    AimTarget = AimPart.Head;
                    AimSmooth = 0;           // instant lock
                    NoRecoil = true;
                    RecoilStrength = 1.0f;   // full compensation, dead-straight spray
                    break;
                case MenuMode.Legit:
                    Aimbot = true;
                    SilentAim = false;
                    AimTarget = AimPart.Head;
                    AimSmooth = 0.6f;        // smooth lock that passes as human
                    NoRecoil = false;
                    break;
                case MenuMode.Troll:
                    Aimbot = false;
                    SilentAim = false;
                    TriggerBot = false;
                    NoRecoil = false;
                    Bhop = false;
                    JumpSounds = true;       // tung tung tung on every jump
                    RoundEndSound = true;    // custom MVP / round-end sound
                    break;
            }
        }

        private const string FileName = "config.json";
        // case-insensitive so configs saved by older builds (lowercase keys) still load
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        class ConfigData
        {
            public bool Esp { get; set; }
            public bool EspBox { get; set; }
            public bool EspHealthBar { get; set; }
            public bool EspTracer { get; set; }
            public bool EspName { get; set; }
            public bool EspSkeleton { get; set; }
            public float BoneThickness { get; set; }
            public bool Glow { get; set; }
            public bool Aimbot { get; set; }
            public bool SilentAim { get; set; }
            public float AimSmooth { get; set; }
            public float AimFov { get; set; }
            public AimPart AimTarget { get; set; }
            public bool Deathmatch { get; set; }
            public bool NoRecoil { get; set; }
            public float RecoilStrength { get; set; }
            public bool Bhop { get; set; }
            public bool AntiFlash { get; set; }
            public bool CameraFov { get; set; }
            public float CameraFovValue { get; set; }
            public bool TriggerBot { get; set; }
            public int TriggerKey { get; set; }
            public MenuMode Mode { get; set; }
            public bool JumpSounds { get; set; }
            public bool RoundEndSound { get; set; }
            public float[] TeamColor { get; set; } = new float[4];
            public float[] EnemyColor { get; set; } = new float[4];
        }

        public void Save() => SaveTo(Path.Combine(AppContext.BaseDirectory, FileName));

        public void Load() => LoadFrom(Path.Combine(AppContext.BaseDirectory, FileName));

        public void SaveTo(string path)
        {
            try
            {
                var cfg = new ConfigData
                {
                    Esp = Esp,
                    EspBox = EspBox,
                    EspHealthBar = EspHealthBar,
                    EspTracer = EspTracer,
                    EspName = EspName,
                    EspSkeleton = EspSkeleton,
                    BoneThickness = BoneThickness,
                    Glow = Glow,
                    Aimbot = Aimbot,
                    SilentAim = SilentAim,
                    AimSmooth = AimSmooth,
                    AimFov = AimFov,
                    AimTarget = AimTarget,
                    Deathmatch = Deathmatch,
                    NoRecoil = NoRecoil,
                    RecoilStrength = RecoilStrength,
                    Bhop = Bhop,
                    AntiFlash = AntiFlash,
                    CameraFov = CameraFov,
                    CameraFovValue = CameraFovValue,
                    TriggerBot = TriggerBot,
                    TriggerKey = TriggerKey,
                    Mode = Mode,
                    JumpSounds = JumpSounds,
                    RoundEndSound = RoundEndSound,
                    TeamColor = ToArray(TeamColor),
                    EnemyColor = ToArray(EnemyColor)
                };
                File.WriteAllText(path, JsonSerializer.Serialize(cfg, JsonOptions));
            }
            catch { }
        }

        public void LoadFrom(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return;

                var cfg = JsonSerializer.Deserialize<ConfigData>(File.ReadAllText(path), JsonOptions);
                if (cfg == null)
                    return;

                Esp = cfg.Esp;
                EspBox = cfg.EspBox;
                EspHealthBar = cfg.EspHealthBar;
                EspTracer = cfg.EspTracer;
                EspName = cfg.EspName;
                EspSkeleton = cfg.EspSkeleton;
                BoneThickness = cfg.BoneThickness;
                Glow = cfg.Glow;
                Aimbot = cfg.Aimbot;
                SilentAim = cfg.SilentAim;
                AimSmooth = cfg.AimSmooth;
                AimFov = cfg.AimFov;
                AimTarget = cfg.AimTarget;
                Deathmatch = cfg.Deathmatch;
                NoRecoil = cfg.NoRecoil;
                RecoilStrength = cfg.RecoilStrength;
                Bhop = cfg.Bhop;
                AntiFlash = cfg.AntiFlash;
                CameraFov = cfg.CameraFov;
                CameraFovValue = cfg.CameraFovValue;
                TriggerBot = cfg.TriggerBot;
                TriggerKey = cfg.TriggerKey;
                Mode = cfg.Mode;
                JumpSounds = cfg.JumpSounds;
                RoundEndSound = cfg.RoundEndSound;
                TeamColor = FromArray(cfg.TeamColor, TeamColor);
                EnemyColor = FromArray(cfg.EnemyColor, EnemyColor);
            }
            catch { }
        }

        // path for a named .config file (created next to the exe)
        public static string ConfigPath(string name)
        {
            string safe = string.Concat(name.Trim().Split(Path.GetInvalidFileNameChars()));
            if (string.IsNullOrEmpty(safe))
                safe = "cfg";
            return Path.Combine(AppContext.BaseDirectory, safe + ".config");
        }

        // names of every .config saved next to the exe (for the load list)
        public static string[] ConfigNames()
        {
            try
            {
                return Directory.GetFiles(AppContext.BaseDirectory, "*.config")
                    .Select(Path.GetFileNameWithoutExtension)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch { return Array.Empty<string>(); }
        }

        private static float[] ToArray(Vector4 v) => new[] { v.X, v.Y, v.Z, v.W };

        // Ignore fully-transparent colors (they'd make the ESP invisible) and
        // fall back to the default instead.
        private static Vector4 FromArray(float[]? arr, Vector4 fallback)
        {
            if (arr != null && arr.Length == 4)
            {
                var v = new Vector4(arr[0], arr[1], arr[2], arr[3]);
                if (v.W > 0.001f || v.X + v.Y + v.Z > 0.001f)
                    return v;
            }
            return fallback;
        }
    }
}
