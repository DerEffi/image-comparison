using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics.CodeAnalysis;
using System.Collections;
using System.Numerics;

namespace ImageComparison.Services
{
    public static class HashService
    {
        public static ulong[] DHash(Image<Rgba32> image, int detail = 8)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            image.Mutate(ctx => ctx
                                .AutoOrient()
                                .Resize(detail + 1, detail)
                                .Grayscale(GrayscaleMode.Bt601));

            int pixelCount = detail * detail;
            int currentHashIndex = 0;
            ulong[] hash = new ulong[(int)Math.Ceiling((double)pixelCount / 64)]; //reserve number of ulongs to hold bits of pixel comparisons

            image.ProcessPixelRows((imageAccessor) =>
            {
                ulong mask = 1UL << 63;

                for (var y = 0; y < detail; y++)
                {
                    Span<Rgba32> row = imageAccessor.GetRowSpan(y);
                    Rgba32 leftPixel = row[0];

                    for (var index = 1; index < detail + 1; index++)
                    {
                        //if current ulong is full, switch to next and reset mask
                        if (mask == 0)
                        {
                            currentHashIndex++;
                            mask = 1UL << 63;
                        }

                        Rgba32 rightPixel = row[index];
                        if (leftPixel.R < rightPixel.R)
                        {
                            hash[currentHashIndex] |= mask;
                        }

                        leftPixel = rightPixel;
                        mask >>= 1;
                    }
                }
            });

            return hash;
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
