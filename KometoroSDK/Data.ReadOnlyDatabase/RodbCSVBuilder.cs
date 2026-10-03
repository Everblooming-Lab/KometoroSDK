using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EverbloomingLab.KometoroSDK.Data.ReadOnlyDatabase
{
    public class RodbCSVBuilder : IDisposable
    {
        private static readonly Dictionary<string, Func<List<string[]>, int, int, string, Column>> _column_factories = new Dictionary<string, Func<List<string[]>, int, int, string, Column>>
        {
            ["byte"] = (raw, cnt, idx, name)
                => new Column<byte>(idx, name, EDataType.Byte, CreateColumnArray(raw, cnt, idx, byte.Parse)),
            ["int"] = (raw, cnt, idx, name)
                => new Column<int>(idx, name, EDataType.Int, CreateColumnArray(raw, cnt, idx, int.Parse)),
            ["float"] = (raw, cnt, idx, name)
                => new Column<float>(idx, name, EDataType.Float, CreateColumnArray(raw, cnt, idx, float.Parse)),
            ["long"] = (raw, cnt, idx, name)
                => new Column<long>(idx, name, EDataType.Long, CreateColumnArray(raw, cnt, idx, long.Parse)),
            ["bool"] = (raw, cnt, idx, name)
                => new Column<bool>(idx, name, EDataType.Boolean, CreateColumnArray(raw, cnt, idx, bool.Parse)),
        }; // string使用查重单独处理

        private readonly HashSet<string> hashStrPool = new HashSet<string>();

        public Rodb Rodb { get; }

        public RodbCSVBuilder(string name) => Rodb = new Rodb(name);

        public void AddTable(string filePath)
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var rdr = new StreamReader(fs);

            var name = Path.GetFileNameWithoutExtension(filePath); // name就是文件名

            rdr.ReadLine(); // skip 1st

            var colNames = rdr.ReadLine()!.Split(',').Select(s => s.Trim()).ToArray(); // header columName
            var colTypes = rdr.ReadLine()!.Split(',').Select(s => s.Trim()).ToArray(); // header columTypes

            var rawData = new List<string[]>();

            while (rdr.ReadLine() is { } line)
            {
                rawData.Add(SplitCSVLine(line));
            }

            var columns = new Column[colTypes.Length];

            for (var i = 0; i < colTypes.Length; i++)
            {
                columns[i] = CreateColumn(rawData, rawData.Count, i, colNames[i], colTypes[i]);
            }

            Rodb.AddTable(CreateTable(name, columns));
        }

        private string Intern(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            if (hashStrPool.TryGetValue(value, out var cached)) return cached;
            hashStrPool.Add(value);
            return value;
        }

        private Column CreateColumn(List<string[]> rawData, int maxCount, int columnIdx, string columnName, string columnType)
        {
            if (columnType == "string") return CreateStringColumn(rawData, maxCount, columnIdx, columnName);

            return _column_factories.TryGetValue(columnType, out var factory)
                ? factory(rawData, maxCount, columnIdx, columnName)
                : throw new NotSupportedException($"Unsupported type: {columnType}");
        }

        private Column CreateStringColumn(List<string[]> rawData, int maxCount, int columnIdx, string columnName)
            => new Column<string>(columnIdx, columnName, EDataType.String, CreateColumnArray(rawData, maxCount, columnIdx, Intern));

        private static T[] CreateColumnArray<T>(List<string[]> rawData, int maxCount, int columnIdx, Func<string, T> parser)
        {
            var arr = new T[maxCount];
            for (var i = 0; i < maxCount; i++)
            {
                arr[i] = parser(rawData[i][columnIdx]);
            }

            return arr;
        }

        private static Table CreateTable(string name, Column[] columns) =>
            columns[0].Type switch
            {
                EDataType.Int    => new Table<int>(name, columns),
                EDataType.String => new Table<string>(name, columns),
                EDataType.Long   => new Table<long>(name, columns),
                _                => throw new NotSupportedException($"Column type {columns[0].Type} is not supported as Primary Key."),
            };

        private static string[] SplitCSVLine(string line)
        {
            var result = new List<string>();
            var current = new System.Text.StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        // 连续两个引号 "" 是转义，代表一个字面量 "
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            current.Append('"');
                            i++; // 跳过第二个引号
                        }
                        else
                        {
                            inQuotes = false; // 引号结束
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else
                {
                    switch (c)
                    {
                        case '"':
                            inQuotes = true;
                            break;
                        case ',':
                            result.Add(current.ToString());
                            current.Clear();
                            break;
                        default:
                            current.Append(c);
                            break;
                    }
                }
            }

            result.Add(current.ToString()); // 最后一个字段
            return result.ToArray();
        }

        public void Dispose()
        {
            hashStrPool.Clear();
        }
    }
}