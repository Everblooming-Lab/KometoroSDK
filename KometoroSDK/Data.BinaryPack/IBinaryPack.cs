using System.IO;

namespace EverbloomingLab.KometoroSDK.Data.BinPack
{
    /// <summary>
    /// 序列化传输对象接口
    /// 请不要直接用实例类型继承该接口
    /// </summary>
    public interface IBinPackObject { }

    /// <summary>
    /// 传输编码
    /// 继承该接口的对象将可以通过BinaryTransfer系统来进行序列化文件编码
    /// </summary>
    public interface IBinPackEncodable : IBinPackObject
    {
        public BinString BinPackObjectName { get; }

        /// <summary>
        /// 对象将通过该方法执行写入到流中
        /// 请在该方法内实现对象的流写入拆解
        /// 通过提供的Stream.Write()系列方法
        /// </summary>
        /// <param name="stream">写入的流</param>
        public void BinaryWrite(BinaryWriter stream);
    }

    /// <summary>
    /// 传输解码 继承该接口的对象将可以通过BinaryTransfer系统来进行序列化文件解码
    /// </summary>
    /// <typeparam name="T">解码对象类型</typeparam>
    public interface IBinPackDecodable<out T> : IBinPackObject
    {
        /// <summary>
        /// 对象将通过该方法执行 从流中读取对应数据进行对象重组
        /// </summary>
        /// <param name="stream">读取的流</param>
        /// <returns>重组后的对象</returns>
        public T BinaryRead(BinaryReader stream);
    }

    /// <summary>
    /// 编码解码器
    /// 继承该接口的对象将可以通过BinaryTransfer系统来进行序列化文件编码和解码
    /// </summary>
    /// <typeparam name="T">解码对象类型</typeparam>
    public interface IBinPackCodable<out T> : IBinPackEncodable, IBinPackDecodable<T> { }
}