using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

[assembly: InternalsVisibleTo("NUnitTest")]

namespace EverbloomingLab.KometoroSDK.Data.BinPack
{
    /// <summary>
    /// 序列化传输流\n
    /// 该流用于传输BinaryTransfer系统进行编码解码使用\n
    /// 一般情况下不需要直接使用该流\n
    /// </summary>
    public class BinPackStream : Stream
    {
        /// <summary>
        /// Aes加密使用的Key，默认值不安全，请在使用前务必进行手动更换
        /// </summary>
        private static byte[] _aesKey = Extensions._Default_Aes_Key_16;

        /// <summary>
        /// Aes加密使用的IV，默认值不安全，请在使用前务必进行手动更换
        /// </summary>
        private static byte[] _aesIv = Extensions._Default_Aes_Iv_16;

        /// <summary>
        /// 加密模式
        /// </summary>
        public EEncryptionMode EncryptionMode { get; private set; }

        /// <summary>
        /// 压缩模式
        /// </summary>
        public ECompressionMode CompressionMode { get; private set; }

        /// <summary>
        /// 压缩模式
        /// </summary>
        public CompressionLevel CompressionLevel { get; private set; }

        private readonly Stream nativeStream;
        private CryptoStream? cryptoStream;
        private GZipStream? gZipStream;
        private ICryptoTransform endecryptor;

        private readonly bool leaveOpen;

        /// <summary>
        /// 最上层流，最终使用该流进行读写
        /// </summary>
        private Stream? upperStream;

        /// <summary>
        /// 一般情况下不需要手动创建该流进行操作。
        /// </summary>
        /// <param name="stream">架设的上层流</param>
        /// <param name="eMode">加密模式</param>
        /// <param name="cMode">压缩模式</param>
        /// <param name="cLevel">压缩级别</param>
        /// <param name="leaveOpen">是否在流关闭时保持底层流打开</param>
        public BinPackStream(Stream stream, EEncryptionMode eMode, ECompressionMode cMode, CompressionLevel cLevel, bool leaveOpen)
        {
            EncryptionMode = eMode;
            CompressionMode = cMode;
            CompressionLevel = cLevel;
            nativeStream = stream;
            this.leaveOpen = leaveOpen;
            Setup();
        }

        /// <summary>
        /// 一般情况下不需要手动创建该流进行操作。
        /// </summary>
        /// <param name="stream">架设的上层流</param>
        /// <param name="eMode">加密模式</param>
        /// <param name="cMode">压缩模式</param>
        /// <param name="cLevel">压缩级别</param>
        /// <param name="leaveOpen">是否在流关闭时保持底层流打开</param>
        /// <param name="aesKey">密钥</param>
        public BinPackStream(Stream stream, EEncryptionMode eMode, ECompressionMode cMode, CompressionLevel cLevel, bool leaveOpen, AES_Key? aesKey)
        {
            EncryptionMode = eMode;
            CompressionMode = cMode;
            CompressionLevel = cLevel;
            nativeStream = stream;
            this.leaveOpen = leaveOpen;
            _aesKey = aesKey?.Key_16 ?? Extensions._Default_Aes_Key_16;
            _aesIv = aesKey?.IV_16   ?? Extensions._Default_Aes_Iv_16;
            Setup();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void Setup()
        {
            upperStream = nativeStream;

            if (EncryptionMode != EEncryptionMode.None)
            {
                //建立加密流
                switch (EncryptionMode)
                {
                    case EEncryptionMode.Encrypt:
                        //建立加密器
                        endecryptor = Aes.Create().CreateEncryptor(_aesKey, _aesIv);
                        cryptoStream = new CryptoStream(upperStream, endecryptor, CryptoStreamMode.Write, true);
                        break;

                    case EEncryptionMode.Decrypt:
                        //建立解密器
                        endecryptor = Aes.Create().CreateDecryptor(_aesKey, _aesIv);
                        cryptoStream = new CryptoStream(upperStream, endecryptor, CryptoStreamMode.Read, true);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(EncryptionMode), EncryptionMode, null);
                }

                upperStream = cryptoStream;
            }

            if (CompressionMode != ECompressionMode.None)
            {
                gZipStream = CompressionMode switch
                {
                    ECompressionMode.Compress   => new GZipStream(upperStream, CompressionLevel, true),
                    ECompressionMode.Decompress => new GZipStream(upperStream, System.IO.Compression.CompressionMode.Decompress, true),
                    _                           => throw new ArgumentOutOfRangeException(nameof(CompressionMode), CompressionMode, null),
                };

                upperStream = gZipStream;
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                gZipStream?.Dispose();
                cryptoStream?.Dispose();

                if (!leaveOpen)
                    nativeStream?.Dispose();
                else
                    nativeStream?.Flush();
            }

            endecryptor?.Dispose();
            cryptoStream = null;
            gZipStream = null;
            upperStream = null;

            base.Dispose(disposing);
        }

        /// <inheritdoc />
        public override void Flush() => upperStream.Flush();

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => upperStream.Seek(offset, origin);

        /// <inheritdoc />
        public override void SetLength(long value) => upperStream.SetLength(value);

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count) => upperStream.Read(buffer, offset, count);

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => upperStream.Write(buffer, offset, count);

        /// <inheritdoc />
        public override bool CanRead => upperStream.CanRead;

        /// <inheritdoc />
        public override bool CanSeek => upperStream.CanSeek;

        /// <inheritdoc />
        public override bool CanWrite => upperStream.CanWrite;

        /// <inheritdoc />
        public override long Length => upperStream.Length;

        /// <inheritdoc />
        public override long Position
        {
            get => upperStream.Position;
            set => upperStream.Position = value;
        }

        /// <summary>
        /// 加密模式
        /// </summary>
        public enum EEncryptionMode
        {
            /// <summary>
            /// 不执行加密解密
            /// </summary>
            None,

            /// <summary>
            /// 执行加密
            /// </summary>
            Encrypt,

            /// <summary>
            /// 执行解密
            /// </summary>
            Decrypt,
        }

        /// <summary>
        /// 压缩模式
        /// </summary>
        public enum ECompressionMode
        {
            /// <summary>
            /// 不执行压缩和解压
            /// </summary>
            None,

            /// <summary>
            /// 执行压缩
            /// </summary>
            Compress,

            /// <summary>
            /// 执行解压
            /// </summary>
            Decompress,
        }
    }
}