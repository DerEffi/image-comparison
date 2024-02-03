using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics.CodeAnalysis;
using System.Collections;
using System.Numerics;
using ImageComparison.Models;

namespace ImageComparison.Services
{
    /// <summary>
    /// Base Algorithm interface to implement specific algorithms
    /// </summary>
    public interface IHashAlgorithm
    {
        public ulong[] Hash(string file);
    }

    /// <summary>
    /// Common functions to process hash values
    /// </summary>
    public static class HashService
    {
        public const int Version = 1;

        /// <summary>
        /// Hash identifier for storage on disk/database/cache
        /// </summary>
        /// <param name="detail"></param>
        /// <param name="algorithm"></param>
        /// <returns></returns>
        public static string GetIdentifier(int detail, HashAlgorithm algorithm)
        {
            return $"V{Version}D{detail}A{(int)algorithm}";
        }

        /// <summary>
        /// Determines similarity score between 2 images (0 - 10000 -> divide by 100 for percent with 2 decimal places)
        /// </summary>
        /// <param name="hash1"></param>
        /// <param name="hash2"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static short Similarity(ulong[] hash1, ulong[] hash2)
        {
            if((hash2 == null) || hash1.Length != hash2.Length)
                throw new ArgumentOutOfRangeException(nameof(hash2));

            int hashLength = hash2.Length * 64;

            return Convert.ToInt16(Math.Floor((double)(hashLength - HammingDistance(hash1, hash2)) * 10000 / hashLength));
        }

        // Get difference between 2 hashes
        private static int HammingDistance(ulong[] hash1, ulong[] hash2)
        {
            int bitcount = 0;
            for(int i = 0; i < hash1.Length; i++)
            {
                bitcount += HammingWeight(hash1[i] ^ hash2[i]);
            }
            return bitcount;
        }

        // Number of bits flipped in given number
        private static int HammingWeight(ulong i)
        {
            i -= ((i >> 1) & 0x5555555555555555UL);
            i = (i & 0x3333333333333333UL) + ((i >> 2) & 0x3333333333333333UL);
            return (int)(unchecked(((i + (i >> 4)) & 0xF0F0F0F0F0F0F0FUL) * 0x101010101010101UL) >> 56);
        }
    }
}
