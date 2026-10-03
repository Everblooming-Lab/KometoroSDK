using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EverbloomingLab.KometoroSDK.Data.BinPack
{
    /// <summary>
    /// 传输包解码器
    /// 大多数情况下请通过该类来完成流编码解码工作
    /// </summary>
    public class BinPackDecoder : IEnumerable
    {
        /// <summary>
        /// 解码组件的方法词典
        /// 需要解码的类型在创建时需要在BinaryTransferObject.Info中写明传输类型，该部分内容在加密解密时不进行加密解密
        /// 需要解码时，需要通过该字典存入解密方法
        /// 解码器通过检索BinaryTransferObject.Info的关键词和该字典中的key进行匹配解码方法，并尝试解码
        /// </summary>
        private static readonly Dictionary<string, Func<BinaryReader, IBinPackObject>> _bp_create_function_dic = new Dictionary<string, Func<BinaryReader, IBinPackObject>>
        {
            { "bp_s", BinString.BinaryRead },
            { "bp_sb", BinStringBundle.BinaryRead },
        };

        /// <summary>
        /// 读取映射字头
        /// </summary>
        private readonly List<string> bp_info_mapping = new List<string>();

        private byte[] decoderBuffer = new byte[1024 * 4]; // 解码时的临时buffer，避免频繁创建销毁

        public const int MAX_DECODER_BUFFER = 1024 * 10240; // 10MB

        public readonly List<string> BinPackObjectNames = new List<string>();

        /// <summary>
        /// 待解码的文件路径
        /// </summary>
        public string FilePath { get; private set; }

        public bool IsChecksumPassed { get; private set; }

        public int Version { get; private set; }

        public bool DecodeSuccess = false;

        public string? ErrorMessage { get; private set; } = "";

        /// <summary>
        /// 传输包信息
        /// </summary>
        private BinPackage.PkgCfg config;

        /// <summary>
        /// 传输包中的IBinaryTransferObject
        /// </summary>
        public readonly List<IBinPackObject?> BinaryObjects = new List<IBinPackObject?>();

        /// <summary>
        /// 传输包时间信息 该信息在编码时创建
        /// </summary>
        public DateTime PackageCreatedTime { get; private set; }

        /// <summary>
        /// 传输包中的BPO数量
        /// </summary>
        public int Count => BinaryObjects.Count;

        /// <summary>
        /// 传输包的索引器
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IBinPackObject this[int index] => BinaryObjects[index];

        public string PackageName => config.Name;
        public string PackageRemark => config.Remark;
        public BinPackStream.EEncryptionMode EncryptionMode => config.EncryptionMode;
        public BinPackStream.ECompressionMode CompressMode => config.CompressMode;
        public CompressionLevel CompressLevel => config.CompressLevel;

        /// <summary>
        /// 新建一个编码用于编码
        /// </summary>
        public BinPackDecoder() { }

        /// <summary>
        /// 新建一个编码用于解码
        /// </summary>
        /// <param name="filePath"></param>
        public BinPackDecoder(string filePath) => FilePath = filePath;

        /// <summary>
        /// 解码
        /// </summary>
        public void Decode()
        {
            // TODO: 当 BinPackStream 支持 Position 追踪时，可以改为直接在 pkgRdr 上做边界校验

            Log.Print?.Invoke($"Starting to decode file: {FilePath}");

            if (!File.Exists(FilePath))
            {
                ErrorMessage += "File is not found\n";
                return;
            }

            using var fileStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read);

            // hdr integrity check
            bool magicPassed;
            (magicPassed, IsChecksumPassed, Version) = CheckIntegrity(fileStream);

            if (!magicPassed)
            {
                ErrorMessage += "This file is not BinaryTransferPackage.\n";
               
                return;
            }

            if (!IsChecksumPassed)
            {
                ErrorMessage += "Checksum validation failed. The file is corrupted or tampered.\n";
                return;
            }

            // hdr config
            (PackageCreatedTime, config) = ReadConfig(fileStream);

            // body 
            using var bpStream = new BinPackStream(fileStream, config.EncryptionMode, config.CompressMode, config.CompressLevel, false);

            using var pkgRdr = new BinaryReader(bpStream, Encoding.UTF8);

            // mapping
            var mapping = BinStringBundle.BinaryRead(pkgRdr);

            // objs
            var count = pkgRdr.ReadInt32();
            for (var i = 0; i < count; i++)
            {
                // mapping 
                var mappingIdx = pkgRdr.ReadByte();
                if (mappingIdx >= mapping.Count) throw new InvalidDataException($"Mapping index {mappingIdx} out of range.");

                // obj
                BinPackObjectNames.Add(mapping[mappingIdx]!);

                // obj length
                var objLen = pkgRdr.ReadInt32();

                // 安全检查，防止恶意包通过伪造长度字段来造成解码器内存溢出
                if (objLen > MAX_DECODER_BUFFER)
                {
                    Log.Print?.Invoke($"Object length {objLen} exceeds maximum decoder buffer size {MAX_DECODER_BUFFER}.");
                    BinaryObjects.Add(BinNullObject._Null);
                    pkgRdr.Read(new byte[objLen], 0, objLen); // 跳过 buffer
                    continue;
                }

                // 确保解码 buffer 足够大
                if (objLen > decoderBuffer.Length) decoderBuffer = new byte[objLen * 2];

                // 读取对象数据到 buffer
                if (!pkgRdr.ReadExact(decoderBuffer, 0, objLen))
                {
                    BinaryObjects.Add(BinNullObject._Null);
                    Log.Print?.Invoke($"Failed to read object data of length {objLen}.");
                    continue;
                }

                // 通过 MemoryStream 和 BinaryReader 来解码对象，避免对 pkgRdr 的位置造成干扰
                var objMs = new MemoryStream(decoderBuffer, 0, objLen, false, true);
                var objRdr = new BinaryReader(objMs, Encoding.UTF8, true);

                IBinPackObject? obj;
                if (_bp_create_function_dic.TryGetValue(mapping[mappingIdx]!, out var cFunc))
                {
                    obj = cFunc.Invoke(objRdr);
                    if (objMs.Position != objLen) obj = null; // 解码方法未正确读取所有数据，可能是解码方法错误或数据损坏
                }
                else
                {
                    //对象无法在解码端找到
                    obj = null;
                    ErrorMessage += $"Unknown type '{mapping[mappingIdx]}' skipped.\n";
                    Log.Print?.Invoke($"No create function found for type '{mapping[mappingIdx]}'. Object skipped.");
                }

                BinaryObjects.Add(obj ?? BinNullObject._Null);
            }

            Log.Print?.Invoke($"Finished decoding file: {FilePath}. Total objects decoded: {BinaryObjects.Count}.");
            DecodeSuccess = true;
        }

        /// <summary>
        /// 异步解码
        /// </summary>
        /// <param name="callback">回调函数</param>
        /// <returns>异步结束后，该BinaryTransferPackageCodec会送入AsyncRequest.Result中。AsyncRequest.AsyncObject也是该BinaryTransferPackageCodec</returns>
        public Task DecodeAsync(Action<BinPackDecoder>? callback = null) =>
            Task.Run(() =>
            {
                Decode();
                callback?.Invoke(this);
            });

        /// <summary>
        /// 添加解码方法
        /// </summary>
        /// <param name="objInfo">传输对象的Info信息，该信息请与对象传输时生成的BinaryTransferObject.Info保持一致</param>
        /// <param name="createFunc">解码方法</param>
        public static void AddBinPackObjCreateFunction(string objInfo, Func<BinaryReader, IBinPackObject> createFunc)
        {
            _bp_create_function_dic.Remove(objInfo);
            _bp_create_function_dic.Add(objInfo, createFunc);
        }

        /// <summary>
        /// 解码文件，仅创建，请手动调用Decode进行解码
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>返回一个编码解码器</returns>
        public static BinPackDecoder ReadBinaryFile(string filePath)
        {
            var bpd = new BinPackDecoder(filePath);
            return bpd;
        }

        /// <summary>
        /// 解码文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="immediatelyDecode">加载文件后立刻开始解码</param>
        /// <returns>返回一个编码解码器</returns>
        public static BinPackDecoder ReadBinaryFile(string filePath, bool immediatelyDecode)
        {
            var bpd = new BinPackDecoder(filePath);
            if (immediatelyDecode) bpd.Decode();
            return bpd;
        }

        /// <inheritdoc />
        public IEnumerator GetEnumerator() => BinaryObjects.GetEnumerator();

        private (bool magicCheck, bool checksum, int version) CheckIntegrity(FileStream fileStream)
        {
            var fileLen = fileStream.Length;

            if (fileLen < 16) return (false, false, -1); // magic hdr

            // hdr check
            using var rdr = new BinaryReader(fileStream, Encoding.UTF8, true);
            var hdr = rdr.ReadBytes(Extensions._Magic_Header.Length);
            var mCheck = ((ReadOnlySpan<byte>)hdr).SequenceEqual(Extensions._Magic_Header);
            if (!mCheck) return (false, false, -1);
            var version = rdr.ReadInt32();
            var isChecksum = rdr.ReadInt32();

            //hdr passed
            var postHdrPos = fileStream.Position;
            var checksumResult = true;

            //checksum
            if (isChecksum == 1)
            {
                // check
                if (fileLen < 48) return (true, false, version); // sha256
                fileStream.Seek(-32, SeekOrigin.End);
                var pkgHash = rdr.ReadBytes(32);

                // compute hash
                using var sha256 = SHA256.Create();
                fileStream.Seek(0, SeekOrigin.Begin);
                var comptHash = l_ComputeHash(sha256, fileStream, fileLen - 32);
                checksumResult = ((ReadOnlySpan<byte>)comptHash).SequenceEqual(pkgHash);
            }

            fileStream.Position = postHdrPos;

            return (true, checksumResult, version);

            static byte[] l_ComputeHash(HashAlgorithm sha, FileStream stream, long length)
            {
                var buffer = new byte[4096];
                long totalRead = 0;
                while (totalRead < length)
                {
                    var toRead = (int)Math.Min(buffer.Length, length - totalRead);
                    var read = stream.Read(buffer, 0, toRead);
                    if (read <= 0) break;
                    sha.TransformBlock(buffer, 0, read, null, 0);
                    totalRead += read;
                }

                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return sha.Hash;
            }
        }

        private (DateTime, BinPackage.PkgCfg) ReadConfig(FileStream fileStream)
        {
            using var confRdr = new BinaryReader(fileStream, Encoding.UTF8, true);

            var cSize = confRdr.ReadInt32();
            var cData = confRdr.ReadBytes(cSize);

            using var ms = new MemoryStream(cData);
            using var rdr = new BinaryReader(ms, Encoding.UTF8, true);

            var ver = rdr.ReadInt32();
            var eMode = (BinPackStream.EEncryptionMode)rdr.ReadInt32();
            var cMode = (BinPackStream.ECompressionMode)rdr.ReadInt32();
            var cLvl = (CompressionLevel)rdr.ReadInt32();
            var createdTime = rdr.ReadDateTime();
            var name = rdr.ReadString();
            var rmk = rdr.ReadString();
            if (cMode == BinPackStream.ECompressionMode.Compress) cMode = BinPackStream.ECompressionMode.Decompress;
            if (eMode == BinPackStream.EEncryptionMode.Encrypt) eMode = BinPackStream.EEncryptionMode.Decrypt;
            var config = new BinPackage.PkgCfg(name, ver, eMode, cMode, cLvl, rmk);
            return (createdTime, config);
        }

        private static void ReadToEnd(BinaryReader reader, byte[] buffer, int count)
        {
            var needToRead = 0;
            while (needToRead < count)
            {
                var read = reader.Read(buffer, needToRead, count - needToRead);
                if (read <= 0) break;
                needToRead += read;
            }
        }
    }
}