using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageComparison.Models
{
    /// <summary>
    /// Data Structure for stored analysis items in database
    /// </summary>
    public class CacheItem
    {
        public string path;
        public ulong scantime;
        public ulong size;
        public byte[]? hash;

        /// <summary>
        /// stored hash as unsigned long array for math calculations
        /// </summary>
        public ulong[] hashArray {
            get {
                if (hash == null)
                    return Array.Empty<ulong>();

                ulong[] result = new ulong[(int)Math.Ceiling((decimal)hash.Length / 8)];

                for (int i = 0; i * 8 < hash.Length; i++)
                    result[i] = BitConverter.ToUInt64(hash, i * 8);

                return result;
            }
        }
    }
}
