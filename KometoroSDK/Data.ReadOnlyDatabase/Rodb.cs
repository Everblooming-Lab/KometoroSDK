using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("NUnitTest")]

namespace EverbloomingLab.KometoroSDK.Data.ReadOnlyDatabase
{
    public class Rodb
    {
        public string Name { get; }
        public int Count => tables.Count;
        public IReadOnlyDictionary<string, Table> Tables => tables;

        private readonly Dictionary<string, Table> tables = new Dictionary<string, Table>();

        public Rodb(string name) => Name = name;

        internal void AddTable(Table table) => tables.Add(table.Name, table);

        public Table GetTable(string tableName) => tables[tableName];

        public Row GetRow(string tableName, int key) => tables[tableName].GetRow(key);
        public Row GetRow(string tableName, long key) => tables[tableName].GetRow(key);
        public Row GetRow(string tableName, string key) => tables[tableName].GetRow(key);
        public Row GetRowByIndex(string rodbName, int rowIndex) => tables[rodbName].GetRowByIndex(rowIndex);

        public T GetValue<T>(string tableName, int key, string columnName) => tables[tableName].GetValue<T>(key, columnName);
        public T GetValue<T>(string tableName, long key, string columnName) => tables[tableName].GetValue<T>(key, columnName);
        public T GetValue<T>(string tableName, string key, string columnName) => tables[tableName].GetValue<T>(key, columnName);

        public T GetValue<T>(string tableName, int key, int columnIndex) => tables[tableName].GetValue<T>(key, columnIndex);
        public T GetValue<T>(string tableName, long key, int columnIndex) => tables[tableName].GetValue<T>(key, columnIndex);
        public T GetValue<T>(string tableName, string key, int columnIndex) => tables[tableName].GetValue<T>(key, columnIndex);
        public T GetValueByRowIndex<T>(string tableName, int rowIndex, string columnName) => tables[tableName].GetValueByRowIndex<T>(rowIndex, columnName);
        public T GetValueByRowIndex<T>(string tableName, int rowIndex, int columnIndex) => tables[tableName].GetValueByRowIndex<T>(rowIndex, columnIndex);
    }

    internal enum EDataType
    {
        Byte,
        Int,
        Long,
        Float,
        String,
        Boolean,
        ByteArray,
    }
}