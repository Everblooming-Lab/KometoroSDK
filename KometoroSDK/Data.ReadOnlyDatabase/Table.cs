using System;
using System.Collections.Generic;

namespace EverbloomingLab.KometoroSDK.Data.ReadOnlyDatabase
{
    public class Table
    {
        public string Name { get; private set; }

        internal Column[] columns { get; private set; }

        protected Dictionary<string, int> columnMapping;

        public int ColumnCount => columns.Length;
        public Column this[int index] => columns[index];
        public Column this[string name] => columns[columnMapping[name]];
        public IReadOnlyCollection<Column> Columns => columns;
        public IReadOnlyDictionary<string, int> ColumnMapping => columnMapping;

        internal T GetValueByRowIndex<T>(string columnName, int rowIdx) => ((Column<T>)columns[columnMapping[columnName]]).Data[rowIdx];

        protected Table(string name, Column[] columns)
        {
            Name = name;
            this.columns = columns;
            columnMapping = new Dictionary<string, int>(columns.Length);

            // 构建columnMapping
            for (var i = 0; i < columns.Length; i++)
            {
                columnMapping[columns[i].Name] = i;
            }
        }

        public Column GetColumn(string name) => columns[columnMapping[name]];
        public Column<T> GetColumn<T>(string name) => (Column<T>)columns[columnMapping[name]];
        public Column GetColumn(int index) => this[index];
        public Column<T> GetColumn<T>(int index) => (Column<T>)this[index];

        public Row GetRow(int key) => ((Table<int>)this).GetRow(key);
        public Row GetRow(long key) => ((Table<long>)this).GetRow(key);
        public Row GetRow(string key) => ((Table<string>)this).GetRow(key);
        public Row GetRowByIndex(int rowIndex) => new Row(rowIndex, this);

        public T GetValue<T>(int key, string columnName) => ((Table<int>)this).GetValue<T>(key, columnName);
        public T GetValue<T>(long key, string columnName) => ((Table<long>)this).GetValue<T>(key, columnName);
        public T GetValue<T>(string key, string columnName) => ((Table<string>)this).GetValue<T>(key, columnName);

        public T GetValue<T>(int key, int columnIndex) => ((Table<int>)this).GetValue<T>(key, columnIndex);
        public T GetValue<T>(long key, int columnIndex) => ((Table<long>)this).GetValue<T>(key, columnIndex);
        public T GetValue<T>(string key, int columnIndex) => ((Table<string>)this).GetValue<T>(key, columnIndex);

        public T GetValueByRowIndex<T>(int rowIndex, string columnName) => GetColumn<T>(columnName).GetValue(rowIndex);
        public T GetValueByRowIndex<T>(int rowIndex, int columnIndex) => this[columnIndex].GetValue<T>(rowIndex);
    }

    public class Table<TKey> : Table
    {
        private readonly Dictionary<TKey, int> rowMapping;

        public Row GetRow(TKey key) => new Row(rowMapping[key], this);

        public T GetValue<T>(TKey key, string columnName) => ((Column<T>)columns[columnMapping[columnName]]).Data[rowMapping[key]];
        public T GetValue<T>(TKey key, int columnIdx) => ((Column<T>)columns[columnIdx]).Data[rowMapping[key]];

        internal Table(string name, Column[] columns) : base(name, columns)
        {
            // 构建rowMapping
            var rowCount = columns[0].DataLength;
            rowMapping = new Dictionary<TKey, int>(rowCount);
            var pkData = ((Column<TKey>)columns[0]).Data;
            for (var i = 0; i < pkData.Length; i++)
            {
                if (!rowMapping.TryAdd(pkData[i], i)) throw new Exception("Key has exist");
            }
        }
    }
}