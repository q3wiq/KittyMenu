using System.Media;

namespace KittyMenu
{
    // Shared audio helpers for the troll sounds. Plays random .wav files from a
    // subfolder next to the exe, or synthesizes PCM into a playable WAV in
    // memory - no temp files, no winmm.
    public static class SoundFx
    {
        static readonly object SoundLock = new();
        static MemoryStream? _stream; // must outlive the SoundPlayer playback

        // returns false when the folder has no wav files (callers decide)
        public static bool PlayRandom(string subfolder)
        {
            string[] files = ListWavs(subfolder);
            if (files.Length == 0)
                return false;

            string pick = files[Random.Shared.Next(files.Length)];
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    using var player = new SoundPlayer(pick);
                    player.PlaySync();
                }
                catch { }
            });
            return true;
        }

        public static string[] ListWavs(string subfolder)
        {
            try
            {
                string dir = Path.Combine(AppContext.BaseDirectory, subfolder);
                if (!Directory.Exists(dir))
                    return Array.Empty<string>();
                return Directory.GetFiles(dir, "*.wav");
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        // plays synthesized 16-bit PCM as a WAV in memory (blocks until done);
        // false if the audio device rejected it
        public static bool PlayPcm(short[] pcm, int sampleRate)
        {
            byte[] wav = BuildWav(pcm, sampleRate);
            try
            {
                lock (SoundLock)
                {
                    _stream?.Dispose();
                    _stream = new MemoryStream(wav);
                    using var player = new SoundPlayer(_stream);
                    player.Load();
                    player.PlaySync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        static byte[] BuildWav(short[] pcm, int rate)
        {
            int dataLen = pcm.Length * 2;
            var wav = new byte[44 + dataLen];

            Ascii(wav, 0, "RIFF");
            PutInt32(wav, 4, 36 + dataLen);
            Ascii(wav, 8, "WAVE");
            Ascii(wav, 12, "fmt ");
            PutInt32(wav, 16, 16);
            PutUInt16(wav, 20, 1);      // PCM
            PutUInt16(wav, 22, 1);      // mono
            PutInt32(wav, 24, rate);
            PutInt32(wav, 28, rate * 2);
            PutUInt16(wav, 32, 2);
            PutUInt16(wav, 34, 16);
            Ascii(wav, 36, "data");
            PutInt32(wav, 40, dataLen);

            Buffer.BlockCopy(pcm, 0, wav, 44, dataLen);
            return wav;
        }

        static void Ascii(byte[] buf, int at, string s)
        {
            for (int i = 0; i < s.Length; i++)
                buf[at + i] = (byte)s[i];
        }

        static void PutUInt16(byte[] buf, int at, ushort v) =>
            (buf[at], buf[at + 1]) = ((byte)(v & 0xFF), (byte)(v >> 8));

        static void PutInt32(byte[] buf, int at, int v) =>
            (buf[at], buf[at + 1], buf[at + 2], buf[at + 3]) =
                ((byte)(v & 0xFF), (byte)((v >> 8) & 0xFF),
                 (byte)((v >> 16) & 0xFF), (byte)((v >> 24) & 0xFF));
    }
}