using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics.CodeAnalysis;
using System.Collections;
using System.Numerics;
using ImageComparison.Models;

namespace ImageComparison.Services
{
    public interface IHashAlgorithm
    {
        public ulong[] Hash(string file);
    }

    public static class HashService
    {
        public const int Version = 1;

        public static string GetIdentifier(int detail, HashAlgorithm algorithm)
        {
            return $"V{Version}D{detail}A{(int)algorithm}";
        }

        public static short Similarity(ulong[] hash1, ulong[] hash2)
        {
            if((hash2 == null) || hash1.Length != hash2.Length)
                throw new ArgumentOutOfRangeException(nameof(hash2));

            int hashLength = hash2.Length * 64;

            return Convert.ToInt16(Math.Floor((double)(hashLength - HammingDistance(hash1, hash2)) * 10000 / hashLength));
        }

        private static int HammingDistance(ulong[] hash1, ulong[] hash2)
        {
            int bitcount = 0;
            for(int i = 0; i < hash1.Length; i++)
            {
                bitcount += HammingWeight(hash1[i] ^ hash2[i]);
            }
            return bitcount;
        }

        private static int HammingWeight(ulong i)
        {
            i -= ((i >> 1) & 0x5555555555555555UL);
            i = (i & 0x3333333333333333UL) + ((i >> 2) & 0x3333333333333333UL);
            return (int)(unchecked(((i + (i >> 4)) & 0xF0F0F0F0F0F0F0FUL) * 0x101010101010101UL) >> 56);
        }
    }
}
