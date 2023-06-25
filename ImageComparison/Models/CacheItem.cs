using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageComparison.Models
{
    public class CacheItem
    {
        public string path;
        public ulong scantime;
        public ulong size;
        public byte[] hash;

        public ulong[] hashArray {
            get {
                ulong[] result = new ulong[(int)Math.Ceiling((decimal)hash.Length / 8)];

                for (int i = 0; i * 8 < hash.Length; i++)
                    result[i] = BitConverter.ToUInt64(hash, i * 8);

                return result;
            }
        }
    }
}
