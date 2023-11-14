using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ImageComparison.Services.Hashs
{
    public class DHashDouble : IHashAlgorithm
    {
        private readonly int width;
        private readonly int height;
        private readonly int hashArraySize;

        public DHashDouble(int detail)
        {
            width = detail + 1;
            height = detail + 1;
            hashArraySize = (int)Math.Ceiling((double)(detail * detail * 2) / 64); //reserve number of ulongs to hold bits of pixel comparisons
        }

        public ulong[] Hash(string file)
        {
            if(file == null)
                throw new ArgumentNullException(nameof(file));

            ulong[] hash = new ulong[hashArraySize];

            using (Image<Rgba32> image = Image.Load<Rgba32>(file))
            {

                if (image == null)
                    throw new ArgumentNullException(nameof(image));

                image.Mutate(ctx => ctx
                    .AutoOrient()
                    .Resize(width, height)
                    .Grayscale(GrayscaleMode.Bt601));
                int currentHashIndex = 0;

                image.ProcessPixelRows((imageAccessor) =>
                {
                    ulong mask = 1UL << 63;
                    Span<Rgba32> lastRow = imageAccessor.GetRowSpan(0);

                    for (var y = 1; y < height; y++)
                    {
                        Span<Rgba32> row = imageAccessor.GetRowSpan(y);
                        Rgba32 leftPixel = row[0];

                        for (var index = 1; index < width; index++)
                        {
                            //if current ulong is full, switch to next and reset mask
                            if (mask == 0)
                            {
                                currentHashIndex++;
                                mask = 1UL << 63;
                            }

                            //compare pixel to the left
                            Rgba32 rightPixel = row[index];
                            if (leftPixel.R < rightPixel.R)
                            {
                                hash[currentHashIndex] |= mask;
                            }

                            leftPixel = rightPixel;
                            mask >>= 1;

                            //compare pixel above
                            if (rightPixel.R < lastRow[index].R)
                                hash[currentHashIndex] |= mask;
                            mask >>= 1;
                        }

                        lastRow = row;
                    }

                });

            }

            return hash;
        }
    }
}
