using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.CompilerServices;

using SRandom = System.Random;

namespace RealityProgrammer.UnityToolkit.Core.Utility {
    public static class AlgorithmUtilities {
        private static readonly Dictionary<int, SRandom> randoms = new Dictionary<int, SRandom>();
        public static SRandom GetRandomizer(int seed) {
            if (randoms.TryGetValue(seed, out var output)) {
                return output;
            } else {
                SRandom newRandom = new SRandom(seed);
                randoms.Add(seed, newRandom);

                return newRandom;
            }
        }

        public static SRandom InitializeRandomizer() {
            return GetRandomizer(System.Environment.TickCount);
        }

        public static Vector2 Rotate(this Vector2 v, float degrees) {
            float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
            float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

            float tx = v.x;
            float ty = v.y;
            v.x = (cos * tx) - (sin * ty);
            v.y = (sin * tx) + (cos * ty);
            return v;
        }

        public static Vector2 Rotate(this Vector2 v, Vector2 center, float degrees) => center + (v - center).Rotate(degrees);

        public static Vector2 RotatePreCompute(this Vector2 v, float cos, float sin) {
            float tx = v.x;
            float ty = v.y;
            v.x = (cos * tx) - (sin * ty);
            v.y = (sin * tx) + (cos * ty);
            return v;
        }

        public static float NextFloat(this System.Random random) {
            double mantissa = (random.NextDouble() * 2.0) - 1.0;
            // choose -149 instead of -126 to also generate subnormal floats (*)
            double exponent = Math.Pow(2.0, random.Next(-126, 128));
            return (float)(mantissa * exponent);
        }

        public static float NextFloat(this System.Random random, float min, float max) {
            return min + (float)random.NextDouble() * (max - min);
        }

        public static bool PointInsideTriangle(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2) {
            var s = p0.y * p2.x - p0.x * p2.y + (p2.y - p0.y) * p.x + (p0.x - p2.x) * p.y;
            var t = p0.x * p1.y - p0.y * p1.x + (p0.y - p1.y) * p.x + (p1.x - p0.x) * p.y;

            if ((s < 0) != (t < 0))
                return false;

            var a = -p1.y * p2.x + p0.y * (p2.x - p1.x) + p0.x * (p1.y - p2.y) + p1.x * p2.y;

            return a < 0 ? (s <= 0 && s + t >= a) : (s >= 0 && s + t <= a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 MirrorPoint(Vector2 point, Vector2 center) {
            return center + (center - point);
        }

        public static float CalculateHarmonicNumber(uint n) {
            float harmonic = 1;

            for (int i = 2; i <= n; i++) {
                harmonic += 1f / i;
            }

            return harmonic;
        }

        public static void CalculateHarmonicNumbers(float[] buffer) {
            buffer[0] = 1;

            for (int i = 1; i < buffer.Length; i++) {
                buffer[i] += buffer[i - 1] + 1f / (i + 1);
            }
        }

        public static Vector2 RandomPointInsideRectangle(Vector2 center, Vector2 size, float degree, int? randomSeed) {
            var randomizer = randomSeed.HasValue ? GetRandomizer(randomSeed.Value) : InitializeRandomizer();

            Vector2 point = center + new Vector2(-size.x / 2 + (float)(randomizer.NextDouble() * size.x), -size.y / 2 + (float)(randomizer.NextDouble() * size.y));

            return point.Rotate(center, degree);
        }

        public static Vector3 RandomPointInsideRectangle(Vector3 center, Vector3 size, Quaternion rotation, int? randomSeed) {
            var randomizer = randomSeed.HasValue ? GetRandomizer(randomSeed.Value) : InitializeRandomizer();

            Vector3 point = center + new Vector3(-size.x / 2 + (float)(randomizer.NextDouble() * size.x), -size.y / 2 + (float)(randomizer.NextDouble() * size.y), -size.z / 2 + (float)(randomizer.NextDouble() * size.z));

            return center + rotation * (point - center);
        }

        public static Texture2D[] SplitColorChannels(Texture2D original) {
            if (!original.isReadable) {
                Debug.Log("Original texture used for splitting color channel are not readable.");
                return null;
            }

            Texture2D[] maps = new Texture2D[4] {
                new Texture2D(original.width, original.height),
                new Texture2D(original.width, original.height),
                new Texture2D(original.width, original.height),
                new Texture2D(original.width, original.height),
            };

            // Is this really important?
            maps[0].filterMode = original.filterMode;
            maps[1].filterMode = original.filterMode;
            maps[2].filterMode = original.filterMode;
            maps[3].filterMode = original.filterMode;

            var pixels = original.GetPixels32();

            for (int i = 0; i < pixels.Length; i++) {
                maps[0].SetPixel(i % original.width, i / original.width, new Color32(pixels[i].r, 0, 0, 255));
                maps[1].SetPixel(i % original.width, i / original.width, new Color32(0, pixels[i].g, 0, 255));
                maps[2].SetPixel(i % original.width, i / original.width, new Color32(0, 0, pixels[i].b, 255));
                maps[3].SetPixel(i % original.width, i / original.width, new Color32(255, 255, 255, pixels[i].a));
            }

            maps[0].Apply();
            maps[1].Apply();
            maps[2].Apply();
            maps[3].Apply();

            return maps;
        }

        public static Texture2D MergeColorChannels(Texture2D r, Texture2D g, Texture2D b, Texture2D a) {
            if ((r.width ^ g.width ^ b.width ^ a.width) != 0 || (r.height ^ g.height ^ b.height ^ a.height) != 0) {
                Debug.Log("The size of color channel textures are not compatible");
                return null;
            }

            if (!r.isReadable || !g.isReadable || !b.isReadable || !a.isReadable) {
                Debug.Log("One of the color channel texture is not readable");
                return null;
            }

            Texture2D merge = new Texture2D(r.width, r.height);
            merge.filterMode = r.filterMode;

            var reds = r.GetPixels32();
            var greens = g.GetPixels32();
            var blues = b.GetPixels32();
            var alphas = a.GetPixels32();

            var outputs = new Color32[reds.Length];

            for (int i = 0; i < reds.Length; i++) {
                outputs[i] = new Color32(reds[i].r, greens[i].g, blues[i].b, alphas[i].a);
            }

            merge.SetPixels32(outputs);
            merge.Apply();

            return merge;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Fractional(float x) {
            float ax = Math.Abs(x);

            return ax - Mathf.Floor(ax);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Fractional(Vector2 vec) {
            float ax = Math.Abs(vec.x);
            float ay = Math.Abs(vec.y);

            return new Vector2(ax - Mathf.Floor(ax), ay - Mathf.Floor(ay));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Fractional(Vector3 vec) {
            float ax = Math.Abs(vec.x);
            float ay = Math.Abs(vec.y);
            float az = Math.Abs(vec.z);

            return new Vector3(ax - Mathf.Floor(ax), ay - Mathf.Floor(ay), az - Mathf.Floor(az));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Fractional(Vector4 vec) {
            float ax = Math.Abs(vec.x);
            float ay = Math.Abs(vec.y);
            float az = Math.Abs(vec.z);
            float aw = Math.Abs(vec.w);

            return new Vector4(ax - Mathf.Floor(ax), ay - Mathf.Floor(ay), az - Mathf.Floor(az), aw - Mathf.Floor(aw));
        }

        public const uint TotalNumberOfHashCalculation = 1;
        public static readonly Dictionary<uint, Func<uint, uint, uint>> _hashCalculationDictionary = new Dictionary<uint, Func<uint, uint, uint>>() {
            { 0, CalculateHashModel0 },
        };

        public static uint CalculateHashModel0(uint input, uint seed = 0) {
            input ^= 2747636419u;
            input *= 2654435769u;
            input ^= input >> 16;
            input *= 2654435769u;
            input ^= input >> 16;
            input *= 2654435769u;

            input ^= seed * 7919u;
            input *= 2654435769u;
            input ^= seed % 4651u;
            input *= 2654435769u;

            return input;
        }

        public static uint CalculateHash(uint style, uint input, uint seed = 0) {
            if (_hashCalculationDictionary.TryGetValue(style, out var @delegate)) {
                return @delegate.Invoke(input, seed);
            }

            return _hashCalculationDictionary[0].Invoke(input, seed);
        }
    }
}