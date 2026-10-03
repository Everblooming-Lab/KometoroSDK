using System;
using System.Collections.Generic;
using System.Linq;

namespace EverbloomingLab.KometoroSDK.Data.ReadOnlyDatabase
{
    public class RodbMockBuilder
    {
        private readonly Rodb rodb;

        public RodbMockBuilder(string name) =>rodb = new Rodb(name);

        public TableBuilder AddTable(string tableName) => new TableBuilder(this, tableName);

        public Rodb Build() => rodb;

        internal void AddTable(Table table) => rodb.AddTable(table);

        public class TableBuilder
        {
            private readonly RodbMockBuilder rodbBuilder;
            private readonly string tableName;

            private readonly List<(string Name, EDataType Type)> columns = new List<(string Name, EDataType Type)>();
            private readonly List<object[]> rows = new List<object[]>();

            public TableBuilder(RodbMockBuilder rodbBuilder, string tableName)
            {
                this.rodbBuilder = rodbBuilder;
                this.tableName = tableName;
            }

            public TableBuilder AddColumn<T>(string name)
            {
                var type = typeof(T);
                EDataType dataType = type switch
                {
                    _ when type == typeof(int) => EDataType.Int,
                    _ when type == typeof(string) => EDataType.String,
                    _ when type == typeof(bool) => EDataType.Boolean,
                    _ when type == typeof(float) => EDataType.Float,
                    _ when type == typeof(long) => EDataType.Long,
                    _ => throw new NotSupportedException($"The data type '{type.Name}' is not supported."),
                };

                columns.Add((name, dataType));
                return this;
            }

            public TableBuilder AddRow(params object[] values)
            {
                if (values.Length != columns.Count)
                    throw new ArgumentException($"Row value count mismatch. Expected {columns.Count}, but got {values.Length}");

                rows.Add(values);
                return this;
            }

            public RodbMockBuilder Finish()
            {
                if (columns.Count == 0)
                    throw new InvalidOperationException("Table must have at least one column to act as Primary Key.");

                var cols = new Column[columns.Count];
                for (var i = 0; i < columns.Count; i++)
                {
                    cols[i] = CreateColumn(i, columns[i].Name, columns[i].Type);
                }

                Table table = cols[0].Type switch
                {
                    EDataType.Int => new Table<int>(tableName, cols),
                    EDataType.String => new Table<string>(tableName, cols),
                    EDataType.Long => new Table<long>(tableName, cols),
                    _ => throw new NotSupportedException($"Column type {cols[0].Type} is not supported as Primary Key.")
                };

                rodbBuilder.AddTable(table);
                return rodbBuilder;
            }

            private Column CreateColumn(int index, string name, EDataType type)
            {
                // 通过 LINQ 将 object[] 中的拆箱转为强类型数组，用于初始化 Column<T>
                return type switch
                {
                    EDataType.Byte => new Column<byte>(index, name, type, rows.Select(r => (byte)r[index]).ToArray()),
                    EDataType.Int => new Column<int>(index, name, type, rows.Select(r => (int)r[index]).ToArray()),
                    EDataType.Long => new Column<long>(index, name, type, rows.Select(r => (long)r[index]).ToArray()),
                    EDataType.Float => new Column<float>(index, name, type, rows.Select(r => (float)r[index]).ToArray()),
                    EDataType.String => new Column<string>(index, name, type, rows.Select(r => (string)r[index]).ToArray()),
                    EDataType.Boolean => new Column<bool>(index, name, type, rows.Select(r => (bool)r[index]).ToArray()),
                    EDataType.ByteArray => new Column<byte[]>(index, name, type, rows.Select(r => (byte[])r[index]).ToArray()),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

        }
    }
}