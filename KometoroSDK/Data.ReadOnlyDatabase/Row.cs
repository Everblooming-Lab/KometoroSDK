namespace EverbloomingLab.KometoroSDK.Data.ReadOnlyDatabase
{
    public readonly struct Row
    {
        private readonly int rowIndex;
        private readonly Table table;

        public Row(int rowIndex, Table table)
        {
            this.rowIndex = rowIndex;
            this.table = table;
        }

        public T GetValue<T>(int columnIdx) => ((Column<T>)table[columnIdx])[rowIndex];
        public T GetValue<T>(string columnName) => table.GetValueByRowIndex<T>(columnName, rowIndex);
    }
}