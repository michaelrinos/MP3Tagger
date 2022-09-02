using System.Text;

namespace Reflection {

    public static class Compression {
        public static byte[] SerializeAndCompressToBinary<T>(this T item) {
            using (var ms = new System.IO.MemoryStream()) {
                var bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                using (var compressor = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Compress)) {
                    bf.Serialize(compressor, item);
                    compressor.Close();

                    return ms.ToArray();
                }
            }
        }

        public static object DeserializeAndDecompressFromBinary(byte[] bytes) {
            using (var ms = new System.IO.MemoryStream(bytes)) {
                using (var decompressor = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Decompress)) {
                    var bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                    return bf.Deserialize(decompressor);
                }
            }
        }

        public static string Decompress(byte[] bytes) {
            using (var msi = new System.IO.MemoryStream(bytes))
            using (var mso = new System.IO.MemoryStream()) {
                using (var gs = new System.IO.Compression.GZipStream(msi, System.IO.Compression.CompressionMode.Decompress))
                    gs.CopyTo(mso);

                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }
    }
}
