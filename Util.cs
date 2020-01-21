
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Fluent.Socket
{
    internal static class Util
    {
        internal static byte[] ObjectToByteArray(object obj)
        {
            if (obj == null) { return null; }
            var bf = new BinaryFormatter();
            using var ms = new MemoryStream();
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }

        internal static object ByteArrayToObject(byte[] arrBytes)
        {
            using var memStream = new MemoryStream();
            var binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            return binForm.Deserialize(memStream);
        }

        internal static T ByteArrayToObject<T>(byte[] arrBytes)
        {
            return (T)ByteArrayToObject(arrBytes);
        }

        internal static MemoryStream SerializeToStream(object o)
        {
            var stream = new MemoryStream();
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, o);
            return stream;
        }

        internal static object DeserializeFromStream(this MemoryStream stream)
        {
            var formatter = new BinaryFormatter();
            stream.Seek(0, SeekOrigin.Begin);
            return formatter.Deserialize(stream);
        }

        internal static T DeserializeFromStream<T>(this MemoryStream stream) where T : class
        {
            return stream.DeserializeFromStream() as T;
        }
    }
}
