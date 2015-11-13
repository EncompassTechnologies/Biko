using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Communications.Net.Imap.EncodingHelpers
{
    public static class Base64
    {
        public static string ToBase64(byte[] data)
        {
            var builder = new StringBuilder();

            using (var writer = new StringWriter(builder))
            {
                using (var transformation = new ToBase64Transform())
                {
                    var bufferedOutputBytes = new byte[transformation.OutputBlockSize];
                    int i = 0;
                    int inputBlockSize = transformation.InputBlockSize;

                    while (data.Length - i > inputBlockSize)
                    {
                        transformation.TransformBlock(data, i, data.Length - i, bufferedOutputBytes, 0);
                        i += inputBlockSize;
                        writer.Write(Encoding.UTF8.GetString(bufferedOutputBytes, 0, bufferedOutputBytes.Length));
                    }

                    bufferedOutputBytes = transformation.TransformFinalBlock(data, i, data.Length - i);
                    writer.Write(Encoding.UTF8.GetString(bufferedOutputBytes, 0, bufferedOutputBytes.Length));
                    transformation.Clear();
                }

                writer.Close();
            }

            return builder.ToString();
        }

        public static byte[] FromBase64(string s)
        {
            byte[] bytes;

            using (var writer = new MemoryStream())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(s);

                using (var transformation = new FromBase64Transform(FromBase64TransformMode.IgnoreWhiteSpaces))
                {
                    var bufferedOutputBytes = new byte[transformation.OutputBlockSize];
                    int i = 0;

                    while (inputBytes.Length - i > 4)
                    {
                        transformation.TransformBlock(inputBytes, i, 4, bufferedOutputBytes, 0);
                        i += 4;
                        writer.Write(bufferedOutputBytes, 0, transformation.OutputBlockSize);
                    }

                    bufferedOutputBytes = transformation.TransformFinalBlock(inputBytes, i, inputBytes.Length - i);
                    writer.Write(bufferedOutputBytes, 0, bufferedOutputBytes.Length);
                    transformation.Clear();
                }

                writer.Position = 0;
                bytes = writer.ToArray();
                writer.Close();
            }

            return bytes;
        }
    }
}