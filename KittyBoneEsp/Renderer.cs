using System.Numerics;
using ClickableTransparentOverlay;
using ImGuiNET;

namespace KittyMenu
{
    // ImGui overlay: hosts the menu (bound to Settings) and draws the ESP on a
    // transparent, click-through window. Runs on its own thread.
    public class Renderer : Overlay
    {
        private readonly Settings _s;
        private List<Entity> _entities = new();
        private Entity _localPlayer = new();

        private Vector2 _windowLocation = Vector2.Zero; // overlay covers the game window => origin
        private const float MenuWidth = 520f;
        private const float MenuHeight = 500f;
        private bool _styled = false;
        private int _saveCounter = 0;
        private string _configName = "";

        // bone pairs that make up the visible skeleton (drawn as lines)
        private static readonly (BoneIds, BoneIds)[] Skeleton =
        {
            (BoneIds.Neck, BoneIds.Head),
            (BoneIds.Neck, BoneIds.ShoulderLeft),
            (BoneIds.Neck, BoneIds.ShoulderRight),
            (BoneIds.ShoulderLeft, BoneIds.ForeLeft),
            (BoneIds.ShoulderRight, BoneIds.ForeRight),
            (BoneIds.ForeLeft, BoneIds.HandLeft),
            (BoneIds.ForeRight, BoneIds.HandRight),
            (BoneIds.Neck, BoneIds.Waist),
            (BoneIds.Waist, BoneIds.KneeLeft),
            (BoneIds.Waist, BoneIds.KneeRight),
            (BoneIds.KneeLeft, BoneIds.FeetLeft),
            (BoneIds.KneeRight, BoneIds.FeetRight),
        };

        public Vector2 OverlaySize { get; }

        public Renderer(Settings settings, Vector2 overlaySize)
        {
            _s = settings;
            OverlaySize = overlaySize;
            _s.Load();
        }

        // Called from the main loop with a fresh frame snapshot.
        public void UpdateFrame(List<Entity> entities, Entity localPlayer)
        {
            _entities = entities;
            _localPlayer = localPlayer;
        }

        protected override void Render()
        {
            try
            {
                if (!_styled)
                    ApplyTheme(); // ImGui context only exists once rendering starts

                DrawMenu();

                ImGui.SetNextWindowSize(OverlaySize);
                ImGui.SetNextWindowPos(_windowLocation);
                ImGui.Begin("overlay", ImGuiWindowFlags.NoDecoration
                    | ImGuiWindowFlags.NoBackground
                    | ImGuiWindowFlags.NoBringToFrontOnFocus
                    | ImGuiWindowFlags.NoMove
                    | ImGuiWindowFlags.NoInputs
                    | ImGuiWindowFlags.NoCollapse
                    | ImGuiWindowFlags.NoScrollbar
                    | ImGuiWindowFlags.NoScrollWithMouse);
                if (_s.Aimbot) // aim indicator stays visible even with esp off
                    DrawFovCircle();
                if (_s.Esp)
                {
                    DrawTracers();
                    DrawBoxes();
                    DrawSkeletons();
                }
                ImGui.End();

                // save config every ~2s so settings survive crashes / closes
                if (++_saveCounter % 120 == 0)
                    _s.Save();
            }
            catch
            {
                // never let a single bad frame kill the overlay
            }
        }

        private void DrawModeTab()
        {
            int modeIdx = (int)_s.Mode;
            ImGui.RadioButton("off (manual tweaks)", ref modeIdx, 0);
            ImGui.RadioButton("rage", ref modeIdx, 1);
            ImGui.RadioButton("legit", ref modeIdx, 2);
            ImGui.RadioButton("troll", ref modeIdx, 3);

            if (modeIdx != (int)_s.Mode)
                _s.ApplyMode((Settings.MenuMode)modeIdx);

            ImGui.Separator();
            switch (_s.Mode)
            {
                case Settings.MenuMode.Rage:
                    ImGui.TextDisabled("instant headshot aimbot\nsilent aim redirects every shot\nfull no-recoil (dead-straight spray)");
                    break;
                case Settings.MenuMode.Legit:
                    ImGui.TextDisabled("smooth headshot aimbot only\nno silent aim, looks human");
                    break;
                case Settings.MenuMode.Troll:
                    ImGui.TextDisabled("combat features off\njump + round-end play your sounds");
                    break;
                default:
                    ImGui.TextDisabled("no preset - tweak the tabs freely");
                    break;
            }

            ImGui.Separator();
            ImGui.Checkbox("jump sounds", ref _s.JumpSounds);
            ImGui.TextDisabled("drop .wav files into jumpsounds\\\nfolder for custom jump clips");
            ImGui.Checkbox("round end sound", ref _s.RoundEndSound);
            ImGui.TextDisabled("plays once per round at the MVP moment\ndrop .wav files into winsounds\\ folder");
        }

        private void DrawMenu()
        {
            ImGui.SetNextWindowSize(new Vector2(MenuWidth, MenuHeight), ImGuiCond.FirstUseEver);
            // center the menu on the game window on first open
            ImGui.SetNextWindowPos(new Vector2(OverlaySize.X / 2 - MenuWidth / 2, OverlaySize.Y / 2 - MenuHeight / 2), ImGuiCond.FirstUseEver);
            ImGui.Begin("Kitty Menu");
            if (ImGui.BeginTabBar("##main"))
            {
                if (ImGui.BeginTabItem("Mode"))
                {
                    DrawModeTab();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Visuals"))
                {
                    ImGui.Checkbox("esp", ref _s.Esp);
                    ImGui.Checkbox("box", ref _s.EspBox);
                    ImGui.Checkbox("health bar", ref _s.EspHealthBar);
                    ImGui.Checkbox("tracer", ref _s.EspTracer);
                    ImGui.Checkbox("name", ref _s.EspName);
                    ImGui.Checkbox("skeleton", ref _s.EspSkeleton);
                    ImGui.Checkbox("glow", ref _s.Glow);
                    ImGui.Checkbox("camera fov", ref _s.CameraFov);
                    if (_s.CameraFov)
                        ImGui.SliderFloat("fov", ref _s.CameraFovValue, 90f, 110f);
                    ImGui.SliderFloat("bone thickness", ref _s.BoneThickness, 4, 500);
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Aim"))
                {
                    ImGui.Checkbox("aimbot", ref _s.Aimbot);
                    ImGui.Checkbox("silent aim", ref _s.SilentAim);
                    ImGui.Checkbox("deathmatch (ffa)", ref _s.Deathmatch);
                    ImGui.TextDisabled("ffa: aim & trigger at every player, incl. teammates");
                    if (_s.Aimbot || _s.SilentAim)
                    {
                        int targetIdx = (int)_s.AimTarget;
                        if (ImGui.Combo("aim at", ref targetIdx, "Head\0Neck\0Body\0Auto\0"))
                            _s.AimTarget = (Settings.AimPart)targetIdx;
                        ImGui.SliderFloat("aim fov", ref _s.AimFov, 10, 1000);
                        ImGui.SliderFloat("aim smooth", ref _s.AimSmooth, 0.0f, 0.99f);
                    }
                    ImGui.TextDisabled("silent aim redirects the shot on mouse1\n(crosshair doesn't move)");
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Misc"))
                {
                    ImGui.Checkbox("no recoil", ref _s.NoRecoil);
                    if (_s.NoRecoil)
                        ImGui.SliderFloat("recoil strength", ref _s.RecoilStrength, 0.1f, 1.0f);
                    ImGui.Checkbox("bunny hop", ref _s.Bhop);
                    ImGui.Checkbox("anti flash", ref _s.AntiFlash);
                    ImGui.Checkbox("trigger bot", ref _s.TriggerBot);
                    if (_s.TriggerBot)
                        DrawTriggerKeyPicker();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Colors"))
                {
                    if (ImGui.CollapsingHeader("team color"))
                        ImGui.ColorEdit4("##teamcolor", ref _s.TeamColor);
                    if (ImGui.CollapsingHeader("enemy color"))
                        ImGui.ColorEdit4("##enemycolor", ref _s.EnemyColor);
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Configs"))
                {
                    ImGui.InputText("config name", ref _configName, 64);
                    if (ImGui.Button("save config"))
                        _s.SaveTo(Settings.ConfigPath(_configName));
                    ImGui.SameLine();
                    if (ImGui.Button("load config"))
                        _s.LoadFrom(Settings.ConfigPath(_configName));

                    string[] names = Settings.ConfigNames();
                    if (names.Length > 0)
                    {
                        ImGui.Separator();
                        ImGui.Text("saved configs:");
                        foreach (string n in names)
                            if (ImGui.Selectable(n))
                                _s.LoadFrom(Settings.ConfigPath(n));
                    }
                    else
                    {
                        ImGui.TextDisabled("no .config files saved yet");
                    }
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
            ImGui.End();
        }

        private void DrawTriggerKeyPicker()
        {
            string[] keys = { "left mouse", "right mouse", "X", "C", "V", "F", "Shift", "Alt" };
            int[] vks = { 0x01, 0x02, 0x58, 0x43, 0x56, 0x46, 0x10, 0x12 };
            int idx = System.Array.IndexOf(vks, _s.TriggerKey);
            if (idx < 0)
                idx = 0;
            if (ImGui.Combo("trigger key", ref idx, string.Join("\0", keys) + "\0"))
                _s.TriggerKey = vks[idx];
        }

        // enemy/team color used by every ESP element
        private uint EntityColor(Entity entity) =>
            entity.IsEnemy(_localPlayer)
                ? ImGui.ColorConvertFloat4ToU32(_s.EnemyColor)
                : ImGui.ColorConvertFloat4ToU32(_s.TeamColor);

        // screen-space AABB of the player: head bone -> lowest foot bone
        private bool TryGetBox(Entity entity, out Vector2 topLeft, out Vector2 bottomRight)
        {
            topLeft = default;
            bottomRight = default;

            if (!entity.Bones2d.TryGetValue(BoneIds.Head, out Vector2 head) || head.X < 0 || head.Y < 0)
                return false; // head behind camera / off screen

            float feetY = float.NegativeInfinity;
            if (entity.Bones2d.TryGetValue(BoneIds.FeetLeft, out Vector2 fl) && fl.Y > 0)
                feetY = Math.Max(feetY, fl.Y);
            if (entity.Bones2d.TryGetValue(BoneIds.FeetRight, out Vector2 fr) && fr.Y > 0)
                feetY = Math.Max(feetY, fr.Y);
            if (float.IsNegativeInfinity(feetY) || feetY <= head.Y + 1f)
                return false; // no valid feet this frame

            float height = feetY - head.Y;
            float width = height * 0.35f; // player proportions

            topLeft = new Vector2(head.X - width / 2, head.Y);
            bottomRight = new Vector2(head.X + width / 2, feetY);
            return true;
        }

        private void DrawTracers()
        {
            if (!_s.EspTracer || _entities.Count == 0)
                return;

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 bottom = new Vector2(OverlaySize.X / 2, OverlaySize.Y);

            foreach (Entity entity in _entities)
            {
                if (!TryGetBox(entity, out Vector2 tl, out Vector2 br))
                    continue;

                uint color = EntityColor(entity);
                Vector2 feet = new Vector2((tl.X + br.X) / 2, br.Y);
                drawList.AddLine(bottom, feet, color, 2f);
            }
        }

        private void DrawBoxes()
        {
            if (_entities.Count == 0)
                return;

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            foreach (Entity entity in _entities)
            {
                if (!TryGetBox(entity, out Vector2 tl, out Vector2 br))
                    continue;

                uint color = EntityColor(entity);

                if (_s.EspBox)
                    drawList.AddRect(tl, br, color, 0f, 0, 1.5f);

                if (_s.EspHealthBar)
                    DrawHealthBar(drawList, tl, br, entity.Health);

                if (_s.EspName && !string.IsNullOrEmpty(entity.Name))
                    DrawName(drawList, entity.Name, tl, br);
            }
        }

        // health bar pinned to the left edge of the box, drains top->down
        private void DrawHealthBar(ImDrawListPtr drawList, Vector2 tl, Vector2 br, int health)
        {
            float barWidth = 4f;
            float gap = 3f;
            Vector2 barTl = new Vector2(tl.X - barWidth - gap, tl.Y);
            Vector2 barBr = new Vector2(tl.X - gap, br.Y);

            drawList.AddRectFilled(barTl, barBr, 0xCC000000); // dark trough

            int hp = Math.Clamp(health, 0, 100);
            if (hp > 0)
            {
                float fillHeight = (barBr.Y - barTl.Y) * (hp / 100f);
                Vector2 fillTl = new Vector2(barTl.X, barBr.Y - fillHeight);
                drawList.AddRectFilled(fillTl, barBr, HealthColor(hp));
            }

            drawList.AddRect(barTl, barBr, 0xFF000000); // outline
        }

        // green at full hp -> yellow -> red at zero
        private static uint HealthColor(int health)
        {
            float pct = Math.Clamp(health, 0, 100) / 100f;
            return ImGui.ColorConvertFloat4ToU32(new Vector4(1f - pct, pct, 0f, 1f));
        }

        private void DrawName(ImDrawListPtr drawList, string name, Vector2 tl, Vector2 br)
        {
            const float fontSize = 18f; // bigger, easier to read
            ImFontPtr font = ImGui.GetFont();
            Vector2 textSize = font.CalcTextSizeA(fontSize, float.MaxValue, 0f, name);

            Vector2 pos = new Vector2((tl.X + br.X) / 2 - textSize.X / 2, tl.Y - textSize.Y - 4);
            if (pos.Y < 0)
                pos.Y = tl.Y + 4; // top of screen -> drop the label below the box

            // dark backing so the tag pops on any background
            Vector2 pad = new Vector2(3, 2);
            drawList.AddRectFilled(pos - pad, pos + textSize + pad, 0x96000000);

            uint outline = 0xFF000000;
            uint white = 0xFFFFFFFF;

            // 8-way black outline so the white text stays readable on any background
            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    drawList.AddText(font, fontSize, pos + new Vector2(x, y), outline, name);
            drawList.AddText(font, fontSize, pos, white, name);
        }

        private void DrawSkeletons()
        {
            if (_s.EspSkeleton == false || _entities.Count == 0)
                return;

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            foreach (Entity entity in _entities)
            {
                if (!entity.Bones2d.TryGetValue(BoneIds.Head, out Vector2 head))
                    continue;
                if (head.X < 0 || head.Y < 0) // behind camera / off screen
                    continue;

                uint color = EntityColor(entity);

                float thickness = _s.BoneThickness / entity.Distance; // thinner when far away

                foreach (var (a, b) in Skeleton)
                {
                    if (entity.Bones2d.TryGetValue(a, out Vector2 pa)
                        && entity.Bones2d.TryGetValue(b, out Vector2 pb))
                        drawList.AddLine(pa, pb, color, thickness);
                }

                // head circle sized from the neck->head on-screen distance so it
                // stays proportional to the player (smaller when they walk away)
                float headRadius = Math.Max(2f, Vector2.Distance(entity.Bone2d(BoneIds.Neck), head));
                drawList.AddCircle(head, headRadius, color);
            }
        }

        private void DrawFovCircle()
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 center = new Vector2(OverlaySize.X / 2, OverlaySize.Y / 2);
            uint color = ImGui.ColorConvertFloat4ToU32(_s.EnemyColor);
            drawList.AddCircle(center, _s.AimFov, color, 64, 2f);
        }

        private void ApplyTheme()
        {
            // pink & darker pink, semi-transparent, soft rounded corners
            ImGuiStylePtr style = ImGui.GetStyle();
            style.WindowRounding = 8f;
            style.ChildRounding = 6f;
            style.FrameRounding = 5f;
            style.PopupRounding = 6f;
            style.ScrollbarRounding = 6f;
            style.GrabRounding = 5f;
            style.TabRounding = 5f;
            style.WindowBorderSize = 3.5f;
            style.FrameBorderSize = 1f;
            style.WindowPadding = new Vector2(16, 12);
            style.FramePadding = new Vector2(8, 5);
            style.ItemSpacing = new Vector2(10, 8);
            style.ItemInnerSpacing = new Vector2(8, 6);
            style.WindowTitleAlign = new Vector2(0.5f, 0.5f);

            Vector4 windowBg = new Vector4(0.09f, 0.01f, 0.09f, 0.86f);       // dark pink, semi-transparent
            Vector4 childBg = new Vector4(0.14f, 0.02f, 0.13f, 0.55f);
            Vector4 popupBg = new Vector4(0.11f, 0.02f, 0.11f, 0.92f);
            Vector4 border = new Vector4(1f, 0.3f, 0.85f, 1f);                // bright hot pink border
            Vector4 accent = new Vector4(1f, 0.25f, 0.7f, 1f);                // bright pink
            Vector4 darker = new Vector4(0.55f, 0.05f, 0.35f, 0.9f);          // darker pink
            Vector4 hover = new Vector4(1f, 0.4f, 0.8f, 0.85f);
            Vector4 text = new Vector4(1f, 0.85f, 0.95f, 1f);                 // soft pink-white text

            style.Colors[(int)ImGuiCol.WindowBg] = windowBg;
            style.Colors[(int)ImGuiCol.ChildBg] = childBg;
            style.Colors[(int)ImGuiCol.PopupBg] = popupBg;
            style.Colors[(int)ImGuiCol.Border] = border;
            style.Colors[(int)ImGuiCol.BorderShadow] = new Vector4(0, 0, 0, 0);
            style.Colors[(int)ImGuiCol.FrameBg] = darker;
            style.Colors[(int)ImGuiCol.FrameBgHovered] = hover;
            style.Colors[(int)ImGuiCol.FrameBgActive] = accent;
            style.Colors[(int)ImGuiCol.TitleBg] = darker;
            style.Colors[(int)ImGuiCol.TitleBgActive] = accent;
            style.Colors[(int)ImGuiCol.TitleBgCollapsed] = darker;
            style.Colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.13f, 0.02f, 0.12f, 0.9f);
            style.Colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.11f, 0.02f, 0.1f, 0.8f);
            style.Colors[(int)ImGuiCol.ScrollbarGrab] = darker;
            style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = hover;
            style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = accent;
            style.Colors[(int)ImGuiCol.CheckMark] = accent;
            style.Colors[(int)ImGuiCol.SliderGrab] = accent;
            style.Colors[(int)ImGuiCol.SliderGrabActive] = hover;
            style.Colors[(int)ImGuiCol.Button] = darker;
            style.Colors[(int)ImGuiCol.ButtonHovered] = hover;
            style.Colors[(int)ImGuiCol.ButtonActive] = accent;
            style.Colors[(int)ImGuiCol.Header] = darker;
            style.Colors[(int)ImGuiCol.HeaderHovered] = hover;
            style.Colors[(int)ImGuiCol.HeaderActive] = accent;
            style.Colors[(int)ImGuiCol.Separator] = border;
            style.Colors[(int)ImGuiCol.SeparatorHovered] = accent;
            style.Colors[(int)ImGuiCol.SeparatorActive] = accent;
            style.Colors[(int)ImGuiCol.Tab] = darker;
            style.Colors[(int)ImGuiCol.TabHovered] = hover;
            style.Colors[(int)ImGuiCol.TabActive] = accent;
            style.Colors[(int)ImGuiCol.TabUnfocused] = darker;
            style.Colors[(int)ImGuiCol.TabUnfocusedActive] = accent;
            style.Colors[(int)ImGuiCol.Text] = text;
            style.Colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.6f, 0.4f, 0.55f, 1f);
            style.Colors[(int)ImGuiCol.TextSelectedBg] = accent;

            ImGui.GetIO().FontGlobalScale = 1.1f; // keep the ui compact
            _styled = true;
        }
    }
}
