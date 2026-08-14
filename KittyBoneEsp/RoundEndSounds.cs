using System.Runtime.InteropServices;
using Swed64;

namespace KittyMenu
{
    // Troll: plays a custom "winning" sound at the end of every round (the MVP /
    // round-eol moment) instead of the default in-game music. Detection reads
    // CCSGameRules.m_nRoundEndCount (client.dll + dwGameRules + 0xF44), which
    // increments exactly once per completed round - robust and re-arms cleanly.
    //
    // Drop .wav files into <exe dir>\winsounds\ for your own round music; with
    // no files it synthesizes a short ascending win jingle as a placeholder.
    public static class RoundEndSounds
    {
        static int _prevCount;
        static bool _init;
        static bool _warned;
        static DateTime _lastPlay = DateTime.MinValue;
        static DateTime _lastLog = DateTime.MinValue;

        public static void Run(Swed swed, IntPtr client)
        {
            if (client == IntPtr.Zero)
                return;

            IntPtr rules = swed.ReadPointer(client, Offsets.DwGameRules);
            if (rules == IntPtr.Zero)
            {
                _init = false;
                return;
            }

            int count = swed.ReadInt(rules, Offsets.MRoundEndCount);
            if (!_init)
            {
                _prevCount = count;
                _init = true;
                return;
            }

            bool ended = count > _prevCount; // only forward counter = real round end
            _prevCount = count;

            if (!ended || (DateTime.UtcNow - _lastPlay).TotalMilliseconds < 500)
                return;

            _lastPlay = DateTime.UtcNow;
            LogThrottled();

            if (SoundFx.PlayRandom("winsounds"))
                return;
            LogOnce("placeholder jingle (no .wav in winsounds folder)");

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    SoundFx.PlayPcm(SynthJingle(), 22050);
                }
                catch { }
            });
        }

        static void LogThrottled()
        {
            if ((DateTime.UtcNow - _lastLog).TotalMilliseconds < 1000)
                return;
            _lastLog = DateTime.UtcNow;
            try { Console.WriteLine("[round end] round over"); } catch { }
        }

        static void LogOnce(string what)
        {
            if (_warned)
                return;
            _warned = true;
            Console.WriteLine("[round end] first trigger - " + what);
        }

        // quick ascending "ta-da" placeholder: C5 E5 G5 C6 with a soft decay
        static short[] SynthJingle(int sampleRate = 22050)
        {
            double[] notesHZ = { 523.25, 659.25, 783.99, 1046.50 };
            double noteDur = 0.14;
            int total = (int)(sampleRate * noteDur * notesHZ.Length + sampleRate * 0.15);
            var pcm = new short[total];
            int pos = 0;

            foreach (double f in notesHZ)
            {
                int n = (int)(sampleRate * noteDur);
                for (int i = 0; i < n && pos < total; i++, pos++)
                {
                    double t = i / (double)sampleRate;
                    double env = Math.Exp(-t * 18.0);
                    double s = Math.Sin(2 * Math.PI * f * t) * env;
                    pcm[pos] = (short)Math.Clamp(s * short.MaxValue * 0.85,
                        short.MinValue, short.MaxValue);
                }
            }
            return pcm;
        }
    }
}