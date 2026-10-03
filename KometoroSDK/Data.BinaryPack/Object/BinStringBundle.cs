using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EverbloomingLab.KometoroSDK.Data.BinPack
{
    /// <summary>
    /// 传输用复文本，大量文本使用
    /// 在拆解编码自定义类型中，建议写入string类型时通过该类型进行配置
    /// </summary>
    public class BinStringBundle : IBinPackCodable<BinStringBundle>, IEnumerable<string>
    {
        internal static readonly BinString _BinPackObjectName = new BinString("bp_sb");

        private readonly List<BinString> binaryStrings = new List<BinString>();

        /// <summary>
        /// 创建一个空的BSM
        /// </summary>
        public BinStringBundle() { }

        public BinStringBundle(IEnumerable<string> strings) => binaryStrings.AddRange(strings.Select(s => s.ToBinaryString()));

        public BinStringBundle(IEnumerable<BinString> strings) => binaryStrings.AddRange(strings);

        /// <summary>
        /// 添加一条message
        /// </summary>
        /// <param name="message">信息</param>
        public void Add(string message) => binaryStrings.Add(new BinString(message));

        /// <summary>
        /// 添加一条message
        /// </summary>
        /// <param name="binString">信息</param>
        public void Add(BinString binString) => binaryStrings.Add(binString);

        public int GetIndex(BinString binString) => binaryStrings.IndexOf(binString);

        /// <summary>
        /// 获取string的IEnumerable
        /// </summary>
        /// <returns>返回</returns>
        public IEnumerable<string?> GetIEnumerable() => binaryStrings.Select(bs => bs.Message);

        /// <summary>
        /// 通过索引器访问Messages
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public string? this[int index] => binaryStrings[index].Message;

        /// <summary>
        /// 该BinaryStringMasses中的Message数量
        /// </summary>
        public int Count => binaryStrings.Count;

        public BinString BinPackObjectName => _BinPackObjectName;

        /// <inheritdoc />
        public void BinaryWrite(BinaryWriter stream)
        {
            stream.Write(binaryStrings.Count);
            foreach (var binStr in binaryStrings)
            {
                binStr.BinaryWrite(stream);
            }
        }

        /// <summary>
        /// 对象将通过该方法执行 从流中读取对应数据进行对象重组
        /// </summary>
        /// <param name="stream">读取的流</param>
        /// <returns>重组后的对象</returns>
        public static BinStringBundle BinaryRead(BinaryReader stream)
        {
            var bsm = new BinStringBundle();
            var count = stream.ReadInt32();
            for (var i = 0; i < count; i++)
            {
                bsm.Add(BinString.BinaryRead(stream));
            }

            return bsm;
        }

        BinStringBundle IBinPackDecodable<BinStringBundle>.BinaryRead(BinaryReader stream) => BinaryRead(stream);

        /// <inheritdoc />
        public IEnumerator<string> GetEnumerator() => binaryStrings.Select(bs => bs.Message).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}