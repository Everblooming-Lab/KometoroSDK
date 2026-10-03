using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace EverbloomingLab.KometoroSDK.Data.BinPack
{
    /// <summary>
    /// 拓展方法类
    /// </summary>
    public static class Extensions
    {
        public static readonly byte[] _Magic_Header =
        {
            0x89,
            (byte)'K', (byte)'M', (byte)'T', (byte)'R',
            (byte)'B', (byte)'T', (byte)'P',
        };

        public static readonly int _Version = 1;


        /// <summary>
        /// 读取4个int转换为decimal,并提升流位置
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static decimal ReadDecimal(this BinaryReader stream)
        {
            var bits = new[]
            {
                stream.ReadInt32(),
                stream.ReadInt32(),
                stream.ReadInt32(),
                stream.ReadInt32(),
            };
            return new decimal(bits);
        }

        /// <summary>
        /// 读取一个DateTime，8字节，并提升流位置
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static DateTime ReadDateTime(this BinaryReader stream) => new DateTime(stream.ReadInt64());

        /// <summary>
        /// 将一个DateTime写入流，8字节，并提升流位置
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="value"></param>
        public static void WriteDateTime(this BinaryWriter stream, DateTime value)
        {
            stream.Write(value.Ticks);
        }

        /// <summary>
        /// 泛型数组写入
        /// </summary>
        /// <typeparam name="T">自定义类型，需要继承IBinaryTransferCodec&lt;T&gt;</typeparam>
        /// <param name="list">拓展</param>
        /// <param name="stream">写入流</param>
        public static void BinaryWrite<T>(this IList<T> list, BinaryWriter stream) where T : IBinPackCodable<T>
        {
            //数量
            stream.Write(list.Count);
            //逐个写入
            foreach (var binaryTransferCodec in list)
            {
                binaryTransferCodec.BinaryWrite(stream);
            }
        }

        /// <summary>
        /// 泛型List读取
        /// </summary>
        /// <typeparam name="T">自定义类型，需要继承IBinaryTransferCodec&lt;T&gt;</typeparam>
        /// <param name="list">拓展</param>
        /// <param name="stream">流</param>
        /// <returns></returns>
        public static IList BinaryRead<T>(this List<T> list, BinaryReader stream) where T : IBinPackDecodable<T>, new()
        {
            for (var i = 0; i < stream.ReadInt32(); i++)
            {
                list.Add(new T().BinaryRead(stream));
            }

            return list;
        }

        /// <summary>
        /// 非托管集合写入
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="writer"></param>
        public static void BinaryWrite<T>(this ICollection<T> collection, BinaryWriter writer) where T : unmanaged
        {
            //count
            var count = collection.Count;
            writer.Write(count);

            if (count == 0) return;

            // 数组直接开搞
            if (collection is T[] array)
            {
                writer.Write(MemoryMarshal.AsBytes(array.AsSpan()));
                return;
            }

            // 其他List
            var buffer = ArrayPool<T>.Shared.Rent(count); // 租用缓存池
            try
            {
                collection.CopyTo(buffer, 0);
                writer.Write(MemoryMarshal.AsBytes(buffer.AsSpan(0, count)));
            }
            finally
            {
                ArrayPool<T>.Shared.Return(buffer); // 还回去
            }
        }

        /// <summary>
        /// 非托管集合读出
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static T[] BinaryRead<T>(this BinaryReader reader) where T : unmanaged
        {
            var count = reader.ReadInt32();
            if (count == 0) return Array.Empty<T>();

            var array = new T[count];
            var bytes = MemoryMarshal.AsBytes(array.AsSpan());
            return reader.Read(bytes) != bytes.Length ? throw new EndOfStreamException() : array;
        }

        /// <summary>
        /// 转换为BinaryString
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static BinString ToBinaryString(this string str) => new BinString(str);

        /// <summary>
        /// 转换为BinaryString
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static BinString ToBinaryString(this StringBuilder str) => new BinString(str.ToString());

        /// <summary>
        /// 转换为BinaryStringMasses
        /// </summary>
        /// <param name="strs"></param>
        /// <returns></returns>
        public static BinStringBundle ToBinaryStringMasses(this IEnumerable<string> strs) => new BinStringBundle(strs);

        /// <summary>
        /// 将准确长度的字节读取到现有的缓冲区中。
        /// </summary>
        public static void ReadExact(this Stream stream, byte[] buffer, int offset, int count)
        {
            if (buffer                 == null) throw new ArgumentNullException(nameof(buffer));
            if (offset                 < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (count                  < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (buffer.Length - offset < count) throw new ArgumentException("The buffer is too small for the specified offset and count.", nameof(buffer));

            var totalRead = 0;
            while (totalRead < count)
            {
                var bytesRead = stream.Read(buffer, offset + totalRead, count - totalRead);
                if (bytesRead == 0)
                    throw new EndOfStreamException($"Failed to read the requested {count} bytes: reached end of stream after reading {totalRead} bytes.");
                totalRead += bytesRead;
            }
        }

        /// <summary>
        /// 将准确长度的字节读取到现有的缓冲区中。
        /// </summary>
        public static bool ReadExact(this BinaryReader stream, byte[] buffer, int offset, int count)
        {
            if (buffer                 == null) return false;
            if (offset                 < 0) return false;
            if (count                  < 0) return false;
            if (buffer.Length - offset < count) return false;

            var totalRead = 0;
            while (totalRead < count)
            {
                var bytesRead = stream.Read(buffer, offset + totalRead, count - totalRead);
                if (bytesRead == 0)
                    return false;
                totalRead += bytesRead;
            }

            return true;
        }


        private static void WriteValueType<T>(this BinaryWriter writer, T value) where T : struct
        {
            switch (value)
            {
                case int i:     writer.Write(i); break;
                case long l:    writer.Write(l); break;
                case float f:   writer.Write(f); break;
                case double d:  writer.Write(d); break;
                case decimal m: writer.Write(m); break;
                case bool b:    writer.Write(b); break;
                case char c:    writer.Write(c); break;
                case byte by:   writer.Write(by); break;
                case short s:   writer.Write(s); break;
                case uint ui:   writer.Write(ui); break;
                case ushort us: writer.Write(us); break;
                case ulong ul:  writer.Write(ul); break;
                default:        throw new NotSupportedException($"Unsupported type: {typeof(T)}");
            }
        }


        /// <summary>
        /// 默认密钥
        /// 仅用于debug测试，请不要使用。
        /// </summary>
        internal static readonly byte[] _Default_Aes_Key_16 =
        {
            01, 02, 03, 04,
            11, 12, 13, 14,
            21, 22, 23, 24,
            31, 32, 33, 34,
        };

        /// <summary>
        /// 默认密钥IV
        /// 仅用于debug测试，请不要使用。
        /// </summary>
        internal static readonly byte[] _Default_Aes_Iv_16 =
        {
            09, 08, 07, 06,
            49, 48, 47, 46,
            99, 98, 97, 96,
            29, 39, 49, 59,
        };
    }

    /// <summary>
    /// AES的key类
    /// </summary>
    public class AES_Key
    {
        public byte[] Key_16;

        public byte[] IV_16;

        public AES_Key(byte[] key16, byte[] iv16)
        {
            Key_16 = key16;
            IV_16 = iv16;
        }
    }
}