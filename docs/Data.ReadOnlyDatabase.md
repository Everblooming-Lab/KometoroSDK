# KometoroSDK.Data.ReadOnlyDatabase (RODB)

An in-memory, immutable, column-oriented lookup database for game configuration data, loadable from CSV. Part of the KometoroRDK (Rapid Development Kit) by Catmint Works LLC.

> **Showcase Disclaimer** – This is a **read-only showcase** of internal tooling from **Catmint Works LLC**, published under the MIT License to demonstrate our engineering. We provide **no technical support, no SLA, and do not accept pull requests**.

## Overview

- Assembly: `Data.ReadOnlyDatabase` (`netstandard2.1`)
- Namespace: `EverbloomingLab.KometoroSDK.Data.ReadOnlyDatabase`
- Model: `Rodb` → `Table` → `Column` (typed arrays) with `Row` as a lightweight view.

## Architecture & Principles

- **Read-only by construction.** Tables, columns and constructors are `internal`; public API only exposes lookups. Data is only populated by the builders, then never mutated.
- **Columnar storage.** Each column is a `Column<T>` backed by a `T[]`. A table maps column name → index and primary key → row index via dictionaries built once at load time, giving O(1) lookups.
- **First column is the primary key.** Supported key types: `int`, `long`, `string`. Duplicate keys throw (`"Key has exist"`). Other types are rejected with `NotSupportedException`.
- **`Row` is a `readonly struct`** (row index + table reference) – no copy of data.
- **String interning.** `RodbCSVBuilder` deduplicates strings across all tables through a `HashSet<string>` pool; empty/whitespace becomes `string.Empty`.
- **Two builders, same model.** `RodbCSVBuilder` for real data, `RodbMockBuilder` (fluent API) for tests and prototypes.

## Features Breakdown

| Component | Description |
|---|---|
| `Rodb` | Named collection of tables: `GetTable`, `GetRow`, `GetRowByIndex`, `GetValue<T>` (by key + column name/index), `GetValueByRowIndex<T>`, `Tables`, `Count`. Key overloads for `int`, `long`, `string`. |
| `Table` / `Table<TKey>` | Column access (`this[int]`, `this[string]`, `GetColumn<T>`), row/value lookup, `ColumnMapping`, `ColumnCount`. |
| `Column` / `Column<T>` | Typed storage with `Name`, `Index`, indexer and `GetValue`. |
| `Row` | `GetValue<T>(int columnIdx)` / `GetValue<T>(string columnName)`. |
| `RodbCSVBuilder` | `AddTable(filePath)`; table name = file name without extension. Includes a quote-aware CSV splitter (`""` escapes). `IDisposable` clears the string pool. |
| `RodbMockBuilder` | Fluent `AddTable(name).AddColumn<T>(..).AddRow(..).Finish()` then `Build()`. |
| Supported CSV types | `byte`, `int`, `long`, `float`, `bool`, `string` |

### CSV file format

```
<line 1: ignored (e.g. comments / description)>
Id,Name,Hp,IsBoss          <- column names
int,string,float,bool      <- column types
1,Slime,10.5,false
2,"Dragon, Elder",999,true
```

## Usage Guide

```csharp
using EverbloomingLab.KometoroSDK.Data.ReadOnlyDatabase;

// Load from CSV
using var builder = new RodbCSVBuilder("GameData");
builder.AddTable("Data/Monster.csv");   // table name: "Monster"
Rodb db = builder.Rodb;

string name = db.GetValue<string>("Monster", 1, "Name");
float  hp   = db.GetValue<float>("Monster", 1, "Hp");

Row row = db.GetRow("Monster", 2);
bool boss = row.GetValue<bool>("IsBoss");

// Build in code (tests)
var mock = new RodbMockBuilder("Mock")
	.AddTable("Item")
		.AddColumn<int>("Id")
		.AddColumn<string>("Name")
		.AddRow(1, "Potion")
		.AddRow(2, "Elixir")
		.Finish()
	.Build();

var item = mock.GetValue<string>("Item", 2, "Name"); // "Elixir"
```

> Notes: key/column lookups throw the standard `KeyNotFoundException` / `InvalidCastException` when a key, column or type does not match. `RodbMockBuilder` has no `byte` mapping in `AddColumn<T>`.
