using System.Numerics;
using System.Runtime.InteropServices;
using System.Diagnostics;
using KittyMenu;
using Swed64;

// init swed
Swed swed = new Swed("cs2");

IntPtr client = swed.GetModuleBase("client.dll");
IntPtr engine2 = swed.GetModuleBase("engine2.dll");

// anchor the overlay to the game window's client area so the ESP lines up
// whether the game is windowed, borderless, or fullscreen at any resolution
Vector2 windowPos = Vector2.Zero;
Vector2 screen = new Vector2(1920, 1080);
if (!GameWindow.TryGetClientRect(out windowPos, out screen))
{
    // fallback: engine-reported resolution (may just be the desktop resolution)
    Vector2 res = new Vector2(swed.ReadInt(engine2, 0x9118D0), swed.ReadInt(engine2, 0x9118D4));
    if (res.X >= 1 && res.Y >= 1)
        screen = res;
    else
        screen = new Vector2(1920, 1080); // last-resort fallback
    windowPos = Vector2.Zero;
}

// imgui overlay + persisted settings
Settings settings = new Settings();
Renderer renderer = new Renderer(settings, screen);
renderer.Start().Wait();

// move the native overlay window over the game's client area and size it to
// match, so the overlay's (0,0) equals the game's origin and the ESP is
// neither clipped nor offset
renderer.Size = new System.Drawing.Size((int)screen.X, (int)screen.Y);
renderer.Position = new System.Drawing.Point((int)windowPos.X, (int)windowPos.Y);

// save settings one last time when the app closes
AppDomain.CurrentDomain.ProcessExit += (s, e) => settings.Save();

// game state
Reader reader = new Reader(swed, client, screen);
Entity localPlayer = new Entity();

// main loop
while (true)
{
    try
    {
        // refresh the local player
        localPlayer.PawnAddress = reader.GetLocalPlayerPawn();
        if (localPlayer.PawnAddress != IntPtr.Zero)
        {
            localPlayer.Team = swed.ReadInt(localPlayer.PawnAddress, Offsets.MTeamNum);
            localPlayer.Origin = swed.ReadVec(localPlayer.PawnAddress, Offsets.MVOldOrigin);
        }

        // read the view matrix + player list for this frame
        IntPtr entityList = swed.ReadPointer(client, Offsets.DwEntityList);
        Matrix4x4 viewMatrix = reader.ReadViewMatrix();
        List<Entity> entities = reader.ReadEntities(entityList, localPlayer, viewMatrix);

        // hand a fresh snapshot to the overlay thread
        renderer.UpdateFrame(entities, localPlayer);

        // run enabled features
        Features.Run(swed, client, entityList, settings, localPlayer, entities, screen);

        Thread.Sleep(3);
    }
    catch (Exception ex)
    {
        // transient memory-read errors shouldn't kill the tool - log and keep going
        try { Console.WriteLine("read error: " + ex.Message); } catch { }
        Thread.Sleep(50);
    }
}

// Finds the CS2 game window's client area on screen so the overlay can match
// it exactly (fixes the ESP being offset/clipped when the game isn't a
// 1920x1080 fullscreen window at the origin).
static class GameWindow
{
    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    struct POINT { public int X, Y; }

    [DllImport("user32.dll")]
    static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);

    // true + the game's client position/size in screen px, false if cs2
    // isn't running / its window isn't visible yet
    public static bool TryGetClientRect(out Vector2 position, out Vector2 size)
    {
        position = Vector2.Zero;
        size = Vector2.Zero;

        foreach (Process p in Process.GetProcessesByName("cs2"))
        {
            IntPtr hwnd = p.MainWindowHandle;
            if (hwnd == IntPtr.Zero)
                continue;
            if (!GetClientRect(hwnd, out RECT rc))
                continue;

            POINT origin = new POINT { X = 0, Y = 0 };
            if (!ClientToScreen(hwnd, ref origin))
                continue;

            int w = rc.Right - rc.Left;
            int h = rc.Bottom - rc.Top;
            if (w < 1 || h < 1)
                continue;

            position = new Vector2(origin.X, origin.Y);
            size = new Vector2(w, h);
            return true;
        }

        return false;
    }
}
