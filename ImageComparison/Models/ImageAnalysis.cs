namespace ImageComparison.Models
{
    public class ImageAnalysis
    {
        public FileInfo Image { get; set; }
        public ulong[] Hash { get; set; }

        public byte[] HashBlob {
            get {
                byte[] blob = new byte[Hash.Length * 8];

                for (int i = 0; i < Hash.Length; i++) {
                    byte[] bytes = BitConverter.GetBytes(Hash[i]);
                    for(int j = 0; j < bytes.Length; j++)
                        blob[i * 8 + j] = bytes[j];
                }

                return blob;
            }
        }
    }
}
