using System.IO;

namespace EverbloomingLab.KometoroSDK.Data.BinPack
{
    /// <summary>
    /// 传输用文本，大量文本使用
    /// 在拆解编码自定义类型中，建议写入string类型时通过该类型进行配置
    /// </summary>
    public class BinString : IBinPackCodable<BinString>
    {
        internal static readonly BinString _BinPackObjectName = new BinString("bp_s");

        /// <summary>
        /// 记载的message
        /// </summary>
        public readonly string? Message;

        /// <summary>
        /// 创建一个BS
        /// </summary>
        /// <param name="message">string信息</param>
        public BinString(string message) => Message = message;

        public BinString BinPackObjectName => _BinPackObjectName;

        /// <inheritdoc />
        public void BinaryWrite(BinaryWriter stream) => stream.Write(Message ?? string.Empty);

        /// <summary>
        /// 对象将通过该方法执行 从流中读取对应数据进行对象重组
        /// </summary>
        /// <param name="stream">读取的流</param>
        /// <returns>重组后的对象</returns>
        public static BinString BinaryRead(BinaryReader stream) => new BinString(stream.ReadString());

        BinString IBinPackDecodable<BinString>.BinaryRead(BinaryReader stream) => BinaryRead(stream);

        /// <inheritdoc />
        public override string? ToString() => Message;

        public override bool Equals(object? obj)
        {
            if (obj is BinString other) return Message == other.Message;

            return false;
        }

        public override int GetHashCode() => Message?.GetHashCode() ?? 0;

        public static bool operator ==(BinString? left, BinString? right) => Equals(left, right);
        public static bool operator !=(BinString? left, BinString? right) => !Equals(left, right);
    }
}