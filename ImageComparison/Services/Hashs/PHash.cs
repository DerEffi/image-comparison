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
        private static readonly double _sqrt2 = 1 / Math.Sqrt(2);
        
        private readonly int imageSize;
        private readonly int hashArraySize;

        private readonly List<Vector<double>>[] _dctCoeffsSimd;
        private readonly double _sqrt2DivSize;

        public PHash(int detail) {
            imageSize = 64;
            hashArraySize = 1;

            _sqrt2DivSize = Math.Sqrt(2D / imageSize);
            _dctCoeffsSimd = GenerateDctCoeffsSimd();
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
                for (var x = 0; x < 8; x++)
                {
                    for (var y = 0; y < imageSize; y++)
                    {
                        sequence[y] = rows[y, x];
                    }

                    Dct1D_SIMD(sequence, matrix, x, limit: 8);
                }

                // Only use the top 8x8 values.
                var top8X8 = new double[imageSize];
                for (var y = 0; y < 8; y++)
                {
                    for (var x = 0; x < 8; x++)
                    {
                        top8X8[(y * 8) + x] = matrix[y, x];
                    }
                }

                // Get Median.
                var median = CalculateMedian64Values(top8X8);

                // Calculate hash.
                var mask = 1UL << (imageSize - 1);

                for (var i = 0; i < imageSize; i++)
                {
                    //if current ulong is full, switch to next and reset mask
                    if (mask == 0)
                    {
                        currentHashIndex++;
                        mask = 1UL << 63;
                    }
                    
                    if (top8X8[i] > median)
                    {
                        hash[currentHashIndex] |= mask;
                    }

                    mask >>= 1;
                }
            }

            return hash;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double CalculateMedian64Values(IReadOnlyCollection<double> values)
        {
            Debug.Assert(values.Count == 64, "This DCT method works with 64 doubles.");
            return values.OrderBy(value => value).Skip(31).Take(2).Average();
        }

        private List<Vector<double>>[] GenerateDctCoeffsSimd()
        {
            var results = new List<Vector<double>>[imageSize];
            for (var coef = 0; coef < imageSize; coef++)
            {
                var singleResultRaw = new double[imageSize];
                for (var i = 0; i < imageSize; i++)
                {
                    singleResultRaw[i] = Math.Cos(((2.0 * i) + 1.0) * coef * Math.PI / (2.0 * imageSize));
                }

                var singleResultList = new List<Vector<double>>();
                var stride = Vector<double>.Count;
                Debug.Assert(imageSize % stride == 0, "Size must be a multiple of SIMD stride");
                for (var i = 0; i < imageSize; i += stride)
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
        /// <param name="valuesRaw">Should be an array of doubles of length 64.</param>
        /// <param name="coefficients">Coefficients.</param>
        /// <param name="ci">Coefficients index.</param>
        /// <param name="limit">Limit.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Dct1D_SIMD(double[] valuesRaw, double[,] coefficients, int ci, int limit)
        {
            Debug.Assert(valuesRaw.Length == 64, "This DCT method works with 64 doubles.");

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
