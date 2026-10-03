namespace EverbloomingLab.KometoroSDK.Data.BinPack
{
    /// <summary>
    /// 空对象\n
    /// </summary>
    public class BinNullObject : IBinPackObject
    {
        public static readonly BinNullObject _Null = new BinNullObject();

        public static BinNullObject GetNull() => _Null;
    }
}