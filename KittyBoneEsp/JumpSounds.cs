using System.Runtime.InteropServices;
using Swed64;

namespace KittyMenu
{
    // Troll: plays a random jump sound every time the player jumps. Trigger is
    // the space key itself (physical press, no game memory needed), with the
    // game's +jump button as a bonus signal and a small debounce so a single
    // press never double-plays.
    //
    // Drop .wav files into <exe dir>\jumpsounds\ for your own clips (a narrator
    // saying "tung tung tung" etc.); with no files it synthesizes a hollow low
    // "tung" thump in memory and plays it three times.
    public static class JumpSounds
    {
        const int JumpButtonDown = 0x10000; // +jump pressed bit in the button state
        const int VK_SPACE = 0x20;

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        static bool _prevSpace;
        static bool _prevBtnDown;
        static bool _warned;
        static DateTime _lastPlay = DateTime.MinValue;
        static DateTime _lastLog = DateTime.MinValue;

        public static void Run(Swed swed, IntPtr client, Entity local)
        {
            if (local == null || local.PawnAddress == IntPtr.Zero || client == IntPtr.Zero)
                return;

            // primary: the space key itself (a physical press, exactly what the
            // player does to jump) - no game memory involved
            bool spaceNow = (GetAsyncKeyState(VK_SPACE) & 0x8000) != 0;
            bool jumped = spaceNow && !_prevSpace;
            _prevSpace = spaceNow;

            // bonus: the game's +jump button state (catches rebound jump keys).
            // the game can register it a frame after the physical press, so the
            // debounce stops the two signals from double-plaving
            if (!jumped && (DateTime.UtcNow - _lastPlay).TotalMilliseconds > 150)
            {
                int btn = swed.ReadInt(client, Offsets.Jump);
                bool btnDown = (btn & JumpButtonDown) != 0;
                if (btnDown && !_prevBtnDown)
                    jumped = true;
                _prevBtnDown = btnDown;
            }

            if (jumped && (DateTime.UtcNow - _lastPlay).TotalMilliseconds > 150)
                Play();
        }

        static void Play()
        {
            _lastPlay = DateTime.UtcNow;
            LogThrottled();

            if (SoundFx.PlayRandom("jumpsounds"))
                return;
            LogOnce("synth (no .wav in jumpsounds folder)");

            // no wav files: synthesize a "tung" and play it three times on a
            // background thread so the UI never stutters; fall back to the
            // system beeper if the audio device rejects it
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    short[] tung = SynthTung();
                    for (int i = 0; i < 3; i++)
                    {
                        if (!SoundFx.PlayPcm(tung, 22050))
                            Beep();
                        else
                            Thread.Sleep(100);
                    }
                }
                catch { }
            });
        }

        // system beeper fallback (always audible if the machine has one)
        static void Beep()
        {
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    Console.Beep(130, 140);
                    Thread.Sleep(80);
                }
            }
            catch { }
        }

        static void LogThrottled()
        {
            // one console line per jump, capped at 1/sec so rapid hops don't
            // spam - confirms the trigger is firing even if audio is silent
            if ((DateTime.UtcNow - _lastLog).TotalMilliseconds < 1000)
                return;
            _lastLog = DateTime.UtcNow;
            try { Console.WriteLine("[jump sounds] jump detected"); } catch { }
        }

        static void LogOnce(string what)
        {
            if (_warned)
                return;
            _warned = true;
            Console.WriteLine("[jump sounds] first trigger - " + what);
        }

        // brief punchy "tung": sine whose pitch slides 340 -> 110 Hz with a
        // fast attack, plus a softer octave-down layer for weight
        static short[] SynthTung(int sampleRate = 22050)
        {
            double dur = 0.30;
            int n = (int)(sampleRate * dur);
            var pcm = new short[n];

            for (int i = 0; i < n; i++)
            {
                double t = i / (double)sampleRate;
                double f = 340.0 - 230.0 * (t / dur);
                double env = Math.Exp(-t * 22.0);
                double s = Math.Sin(2 * Math.PI * f * t) * env;
                double s2 = Math.Sin(2 * Math.PI * f * 0.5 * t) * env * 0.7;
                double v = (s + s2) * 0.9;
                pcm[i] = (short)Math.Clamp(v * short.MaxValue, short.MinValue, short.MaxValue);
            }
            return pcm;
        }
    }
}