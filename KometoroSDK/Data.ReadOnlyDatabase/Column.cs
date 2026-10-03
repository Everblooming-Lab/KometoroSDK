namespace EverbloomingLab.KometoroSDK.Data.ReadOnlyDatabase
{
    public abstract class Column
    {
        public int Index { get; private set; }
        public string Name { get; private set; }
        internal EDataType Type { get; private set; }

        internal abstract int DataLength { get; }

        internal Column(int index, string name, EDataType type)
        {
            Index = index;
            Name = name;
            Type = type;
        }

        public T GetValue<T>(int index) => ((Column<T>)this)[index];
    }

    public class Column<T> : Column
    {
        internal readonly T[] Data;

        internal Column(int index, string name, EDataType type, T[] data) : base(index, name, type) => Data = data;

        public T this[int index] => Data[index];
        public T GetValue(int index) => Data[index];
        internal override int DataLength => Data.Length;
    }
}