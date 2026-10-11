using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RobotRescue.EditorTools.Media
{
    /// <summary>
    /// Minimal animated GIF writer (GIF89a, looping, one shared 256-colour palette). The palette is built from the
    /// most common colours across all frames, which suits flat greybox scenes well. No dependencies.
    /// </summary>
    public class GifEncoder
    {
        readonly int width, height, delayCs;
        readonly List<Color32[]> frames = new List<Color32[]>();

        /// <param name="delayCentiseconds">Time per frame in 1/100 s (7 ≈ 14 fps).</param>
        public GifEncoder(int width, int height, int delayCentiseconds)
        {
            this.width = width;
            this.height = height;
            delayCs = delayCentiseconds;
        }

        /// <summary>Pixels bottom-up, as Texture2D.GetPixels32 returns them.</summary>
        public void AddFrame(Color32[] pixels) => frames.Add(pixels);

        public void Save(string path)
        {
            var palette = BuildPalette(out var lookup);
            using (var w = new BinaryWriter(File.Create(path)))
            {
                w.Write(new[] { (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a' });
                w.Write((ushort)width);
                w.Write((ushort)height);
                w.Write((byte)0xF7); // global colour table, 256 entries
                w.Write((byte)0);
                w.Write((byte)0);
                foreach (var c in palette) { w.Write(c.r); w.Write(c.g); w.Write(c.b); }

                // Loop forever.
                w.Write((byte)0x21); w.Write((byte)0xFF); w.Write((byte)11);
                w.Write(System.Text.Encoding.ASCII.GetBytes("NETSCAPE2.0"));
                w.Write((byte)3); w.Write((byte)1); w.Write((ushort)0); w.Write((byte)0);

                var indices = new byte[width * height];
                foreach (var frame in frames)
                {
                    w.Write((byte)0x21); w.Write((byte)0xF9); w.Write((byte)4);
                    w.Write((byte)0x04);
                    w.Write((ushort)delayCs);
                    w.Write((byte)0);
                    w.Write((byte)0);

                    w.Write((byte)0x2C);
                    w.Write((ushort)0); w.Write((ushort)0);
                    w.Write((ushort)width); w.Write((ushort)height);
                    w.Write((byte)0);

                    // Flip to top-down while mapping to palette indices.
                    for (int y = 0; y < height; y++)
                    {
                        int src = (height - 1 - y) * width, dst = y * width;
                        for (int x = 0; x < width; x++) indices[dst + x] = lookup[Key(frame[src + x])];
                    }
                    WriteLzw(w, indices);
                }
                w.Write((byte)0x3B);
            }
        }

        static int Key(Color32 c) => ((c.r >> 3) << 10) | ((c.g >> 3) << 5) | (c.b >> 3);

        static Color32 FromKey(int k) => new Color32(
            (byte)(((k >> 10) & 31) * 255 / 31), (byte)(((k >> 5) & 31) * 255 / 31), (byte)((k & 31) * 255 / 31), 255);

        /// <summary>Popularity palette over 15-bit colours, then a nearest-colour lookup for all 32768 keys.</summary>
        Color32[] BuildPalette(out byte[] lookup)
        {
            var counts = new int[32768];
            foreach (var f in frames)
                for (int i = 0; i < f.Length; i += 3) counts[Key(f[i])]++;

            var keys = new List<int>();
            for (int k = 0; k < counts.Length; k++) if (counts[k] > 0) keys.Add(k);
            keys.Sort((a, b) => counts[b].CompareTo(counts[a]));

            // Greedy pick: most popular first, skipping near-duplicates so rare-but-distinct colours get a slot.
            var palette = new List<Color32>();
            foreach (int min in new[] { 3, 1, 0 })
            {
                foreach (int k in keys)
                {
                    if (palette.Count >= 256) break;
                    var c = FromKey(k);
                    bool close = false;
                    foreach (var p in palette)
                        if (Dist(p, c) <= min * min * 64) { close = true; break; }
                    if (!close) palette.Add(c);
                }
                if (palette.Count >= 256) break;
            }
            while (palette.Count < 256) palette.Add(new Color32(0, 0, 0, 255));

            // Every key gets a mapping: colours are sampled every 3rd pixel, so some never got counted.
            lookup = new byte[32768];
            for (int k = 0; k < 32768; k++)
            {
                var c = FromKey(k);
                int best = 0, bestD = int.MaxValue;
                for (int i = 0; i < 256; i++)
                {
                    int d = Dist(palette[i], c);
                    if (d < bestD) { bestD = d; best = i; if (d == 0) break; }
                }
                lookup[k] = (byte)best;
            }
            return palette.ToArray();
        }

        static int Dist(Color32 a, Color32 b)
        {
            int dr = a.r - b.r, dg = a.g - b.g, db = a.b - b.b;
            return dr * dr * 3 + dg * dg * 4 + db * db * 2;
        }

        // ------------------------------------------------------------------ LZW

        static void WriteLzw(BinaryWriter w, byte[] pixels)
        {
            const int minCodeSize = 8;
            w.Write((byte)minCodeSize);
            int clear = 1 << minCodeSize, eoi = clear + 1;
            var dict = new Dictionary<int, int>(4096);
            int codeSize = minCodeSize + 1, next = eoi + 1;
            var output = new BlockWriter(w);

            output.Write(clear, codeSize);
            int prefix = pixels[0];
            for (int i = 1; i < pixels.Length; i++)
            {
                int k = pixels[i];
                int key = (prefix << 8) | k;
                if (dict.TryGetValue(key, out int code)) { prefix = code; continue; }

                output.Write(prefix, codeSize);
                if (next < 4096)
                {
                    dict[key] = next++;
                    if (next > (1 << codeSize) && codeSize < 12) codeSize++;
                }
                else
                {
                    output.Write(clear, codeSize);
                    dict.Clear();
                    codeSize = minCodeSize + 1;
                    next = eoi + 1;
                }
                prefix = k;
            }
            output.Write(prefix, codeSize);
            output.Write(eoi, codeSize);
            output.Flush();
            w.Write((byte)0);
        }

        /// <summary>Packs variable-width codes LSB-first into 255-byte sub-blocks.</summary>
        class BlockWriter
        {
            readonly BinaryWriter w;
            readonly byte[] block = new byte[255];
            int blockLen, bitBuffer, bitCount;

            public BlockWriter(BinaryWriter w) => this.w = w;

            public void Write(int code, int size)
            {
                bitBuffer |= code << bitCount;
                bitCount += size;
                while (bitCount >= 8)
                {
                    Put((byte)(bitBuffer & 0xFF));
                    bitBuffer >>= 8;
                    bitCount -= 8;
                }
            }

            void Put(byte b)
            {
                block[blockLen++] = b;
                if (blockLen == 255) Emit();
            }

            void Emit()
            {
                if (blockLen == 0) return;
                w.Write((byte)blockLen);
                w.Write(block, 0, blockLen);
                blockLen = 0;
            }

            public void Flush()
            {
                if (bitCount > 0) Put((byte)(bitBuffer & 0xFF));
                bitBuffer = 0;
                bitCount = 0;
                Emit();
            }
        }
    }
}
