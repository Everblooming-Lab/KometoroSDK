using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using static EverbloomingLab.KometoroSDK.Data.BinPack.BinPackStream;

namespace EverbloomingLab.KometoroSDK.Data.BinPack
{
    /// <summary>
    /// 传输用包
    /// 创建该包以用于序列化传输文件
    /// </summary>
    public class BinPackage
    {
        private const string DEFAULT_NAME = "kmtr_bin_trans_pkg";
        private const int DEFAULT_PKG_VERSION = 0;

        private const BinPackStream.EEncryptionMode DEFAULT_ENCRYPTION_MODE = BinPackStream.EEncryptionMode.None;
        private const BinPackStream.ECompressionMode DEFAULT_COMPRESSION_MODE = BinPackStream.ECompressionMode.None;
        private const CompressionLevel DEFAULT_COMPRESS_LEVEL = CompressionLevel.Optimal;

        public bool IsChecksum;

        private readonly PkgCfg config;

        /// <summary>
        /// 包中含有的BinaryTransferObject
        /// </summary>
        public readonly List<IBinPackEncodable> BinPackObjList = new List<IBinPackEncodable>();

        public BinPackage(string name,
                          int version,
                          BinPackStream.EEncryptionMode encryptionMode,
                          BinPackStream.ECompressionMode compressMode,
                          CompressionLevel compressLevel,
                          string remark) =>
            config = new PkgCfg(name,
                                version,
                                encryptionMode,
                                compressMode,
                                compressLevel,
                                remark);

        public BinPackage(string name, int version, string remark = "") =>
            config = new PkgCfg(name,
                                version,
                                DEFAULT_ENCRYPTION_MODE,
                                DEFAULT_COMPRESSION_MODE,
                                DEFAULT_COMPRESS_LEVEL,
                                remark);
        
        public BinPackage(
            BinPackStream.EEncryptionMode encryptionMode,
            BinPackStream.ECompressionMode compressMode,
            string remark = "",
            CompressionLevel compressLevel = DEFAULT_COMPRESS_LEVEL) =>
            config = new PkgCfg(DEFAULT_NAME,
                                DEFAULT_PKG_VERSION,
                                encryptionMode,
                                compressMode,
                                compressLevel,
                                remark);

        public BinPackage(
            string name,
            int version,
            BinPackStream.EEncryptionMode encryptionMode,
            BinPackStream.ECompressionMode compressMode,
            string remark = "",
            CompressionLevel compressLevel = DEFAULT_COMPRESS_LEVEL) =>
            config = new PkgCfg(name,
                                version,
                                encryptionMode,
                                compressMode,
                                compressLevel,
                                remark);

        /// <summary>
        /// 添加一个BinaryTransferObject
        /// </summary>
        /// <param name="info">Bp的info信息，该字段将用于解码器匹配解码方法，请确保和解码器字典BinaryTransferCodec._Bp_CREATE_FUNCTION_DIC中的key保持一致</param>
        /// <param name="binPackObject">可传输对象</param>
        public void Add(IBinPackEncodable binPackObject) => BinPackObjList.Add(binPackObject);

        /// <summary>
        /// 将传输包写出到文件中
        /// </summary>
        public void WriteToFile(string filePath)
        {
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);

            if (IsChecksum)
            {
                using var hasher = SHA256.Create();
                using (var hashStream = new CryptoStream(fileStream, hasher, CryptoStreamMode.Write, true))
                {
                    using (var writer = new BinaryWriter(hashStream, Encoding.UTF8, true))
                    {
                        WriteHeader(writer);
                        writer.Flush();
                    }

                    WriteBody(hashStream);
                }

                var integrityHash = hasher.Hash;
                fileStream.Write(integrityHash, 0, integrityHash.Length);
            }
            else
            {
                using var writer = new BinaryWriter(fileStream, Encoding.UTF8, true);
                WriteHeader(writer);
                writer.Flush();
                WriteBody(fileStream);
            }

            fileStream.Flush();
            Log.Print?.Invoke($"BinPackage Write successfully. Package Name: {config.Name}, Remark: {config.Remark}, File path: {filePath}");
        }

        private void WriteHeader(BinaryWriter writer)
        {
            writer.Write(Extensions._Magic_Header); // 8 魔数
            writer.Write(Extensions._Version);      // 4 版本号
            writer.Write(IsChecksum ? 1 : 0);       // 4 是否包含校验码

            // 4+4+4+4+8+变长 包配置 写入configs
            using var ms = new MemoryStream();
            using var memWriter = new BinaryWriter(ms, Encoding.UTF8, true);

            config.Write(memWriter);
            var configData = ms.ToArray();
            writer.Write(configData.Length);
            writer.Write(configData);
            writer.Flush();
        }

        private void WriteBody(Stream stream)
        {
            using var bts = new BinPackStream(stream, config.EncryptionMode, config.CompressMode, config.CompressLevel, true);
            using var writer = new BinaryWriter(bts, Encoding.UTF8, true);

            //写入包头BinaryTransferObjectName映射
            var infoMapping = new BinStringBundle(BinPackObjList.Select(o => o.BinPackObjectName.Message!).Distinct());
            if (infoMapping.Count >= byte.MaxValue) throw new Exception("Object type is over maximum 255");
            infoMapping.BinaryWrite(writer);

            writer.Write(BinPackObjList.Count);

            //写入内存计算大小
            using var memStream = new MemoryStream();
            using var memWriter = new BinaryWriter(memStream, Encoding.UTF8, true);

            foreach (var obj in BinPackObjList)
            {
                //写mapping
                var idx = (byte)infoMapping.GetIndex(obj.BinPackObjectName);
                writer.Write(idx);

                //检查obj大小
                memStream.SetLength(0);
                memStream.Position = 0;
                obj.BinaryWrite(memWriter);
                memWriter.Flush();

                //写入大小
                var len = (int)memStream.Length;
                writer.Write(len);

                //写入对象
                writer.Write(memStream.GetBuffer(), 0, len);
            }

            writer.Flush();
        }

        internal readonly struct PkgCfg
        {
            public readonly string Name;
            public readonly string Remark;
            public readonly int Version;
            public readonly BinPackStream.EEncryptionMode EncryptionMode;
            public readonly BinPackStream.ECompressionMode CompressMode;
            public readonly CompressionLevel CompressLevel;

            public PkgCfg(string name,
                          int version,
                          BinPackStream.EEncryptionMode encryptionMode,
                          BinPackStream.ECompressionMode compressMode,
                          CompressionLevel compressLevel,
                          string remark)
            {
                Name = name;
                Version = version;
                EncryptionMode = encryptionMode;
                CompressMode = compressMode;
                CompressLevel = compressLevel;
                Remark = remark ?? "";
            }

            public void Write(BinaryWriter writer)
            {
                writer.Write(Version);
                writer.Write((int)EncryptionMode);
                writer.Write((int)CompressMode);
                writer.Write((int)CompressLevel);
                writer.WriteDateTime(DateTime.Now);
                writer.Write(Name);
                writer.Write(Remark);
            }
        }
    }
}