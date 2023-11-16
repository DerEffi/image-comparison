using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ImageComparison.Services.Hashs
{
    public class PHash : IHashAlgorithm
    {
        private readonly static double _sqrt2 = 1 / Math.Sqrt(2);
        private readonly static int stride = Vector<double>.Count;
        private readonly double _sqrt2DivSize;

        private readonly int detail;
        private readonly int imageSize;
        private readonly int hashSize;
        private readonly int hashArraySize;
        private readonly int firstTerm;

        private readonly List<Vector<double>>[] _dctCoeffsSimd;

        public PHash(int detail) {
            this.detail = detail;
            this.imageSize = detail * 8;
            while (imageSize % stride != 0)
                imageSize++;
            this.hashSize = detail * detail;
            this.hashArraySize = (int)Math.Ceiling((double)hashSize / 64);
            this.firstTerm = imageSize / 2 - 1;

            _sqrt2DivSize = Math.Sqrt(2D / imageSize);
            _dctCoeffsSimd = GenerateDctCoeffsSimd(imageSize);
        }

        public ulong[] Hash(string file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            ulong[] hash = new ulong[hashArraySize];
            int currentHashIndex = 0;

            using (Image<Rgba32> image = Image.Load<Rgba32>(file))
            {

                if (image == null)
                    throw new ArgumentNullException(nameof(image));

                image.Mutate(ctx => ctx
                    .AutoOrient()
                    .Resize(imageSize, imageSize)
                    .Grayscale(GrayscaleMode.Bt601));

                var rows = new double[imageSize, imageSize];
                var sequence = new double[imageSize];
                var matrix = new double[imageSize, imageSize];

                // Calculate the DCT for each row.
                for (var y = 0; y < imageSize; y++)
                {
                    for (var x = 0; x < imageSize; x++)
                    {
                        sequence[x] = image[x, y].R;
                    }

                    Dct1D_SIMD(sequence, rows, y, imageSize);
                }

                // Calculate the DCT for each column.
                for (var x = 0; x < imageSize; x++)
                {
                    for (var y = 0; y < imageSize; y++)
                    {
                        sequence[y] = rows[y, x];
                    }

                    Dct1D_SIMD(sequence, matrix, x, limit: detail);
                }

                // Only use the low frequencies (first values deppending on detail).
                double[] lowFreq = new double[hashSize];
                for (var y = 0; y < detail; y++)
                {
                    for (var x = 0; x < detail; x++)
                    {
                        lowFreq[(y * detail) + x] = matrix[y, x];
                    }
                }

                // Get Median.
                var median = lowFreq.OrderBy(value => value).Skip(firstTerm).Take(2).Average();

                // Calculate hash.
                var mask = 1UL << 63;

                for (var i = 0; i < hashSize; i++)
                {
                    //if current ulong is full, switch to next and reset mask
                    if (mask == 0)
                    {
                        currentHashIndex++;
                        mask = 1UL << 63;
                    }
                    
                    if (lowFreq[i] > median)
                    {
                        hash[currentHashIndex] |= mask;
                    }

                    mask >>= 1;
                }
            }

            return hash;
        }

        private static List<Vector<double>>[] GenerateDctCoeffsSimd(int size)
        {
            var results = new List<Vector<double>>[size];
            for (var coef = 0; coef < size; coef++)
            {
                double[] singleResultRaw = new double[size];
                for (var i = 0; i < size; i++)
                {
                    singleResultRaw[i] = Math.Cos(((2.0 * i) + 1.0) * coef * Math.PI / (2.0 * size));
                }

                var singleResultList = new List<Vector<double>>();
                for (var i = 0; i < size; i += stride)
                {
                    var v = new Vector<double>(singleResultRaw, i);
                    singleResultList.Add(v);
                }

                results[coef] = singleResultList;
            }

            return results;
        }

        /// <summary>
        /// One dimensional Discrete Cosine Transformation.
        /// </summary>
        /// <param name="valuesRaw">Should be an array of doubles of length of imageSize.</param>
        /// <param name="coefficients">Coefficients.</param>
        /// <param name="ci">Coefficients index.</param>
        /// <param name="limit">Limit.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Dct1D_SIMD(double[] valuesRaw, double[,] coefficients, int ci, int limit)
        {
            var valuesList = new List<Vector<double>>();
            var stride = Vector<double>.Count;
            for (var i = 0; i < valuesRaw.Length; i += stride)
            {
                valuesList.Add(new Vector<double>(valuesRaw, i));
            }

            for (var coef = 0; coef < limit; coef++)
            {
                for (var i = 0; i < valuesList.Count; i++)
                {
                    coefficients[ci, coef] += Vector.Dot(valuesList[i], _dctCoeffsSimd[coef][i]);
                }

                coefficients[ci, coef] *= _sqrt2DivSize;
                if (coef == 0)
                {
                    coefficients[ci, coef] *= _sqrt2;
                }
            }
        }
    }
}
