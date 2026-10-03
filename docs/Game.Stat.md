# KometoroSDK.Game.Stat

An RPG-style stat system: clamped numeric stats, dynamic (computed) stats, and modifier pipelines with flat and percentage bonuses. Part of the KometoroRDK (Rapid Development Kit) by Catmint Works LLC.

> **Showcase Disclaimer** – This is a **read-only showcase** of internal tooling from **Catmint Works LLC**, published under the MIT License to demonstrate our engineering. We provide **no technical support, no SLA, and do not accept pull requests**.

## Overview

- Assembly: `Game.Stat` (`netstandard2.1`)
- Namespace: `EverbloomingLab.KometoroSDK.Game`
- Core ideas: `IStat` (value + bounds + change event), `RawValue` (tolerant double wrapper), `StatModifier` (aggregates `IStatModifySource`s), `StatFactory` (the intended way to create stats), `StatSet` (container).

## Architecture & Principles

- **Factory-only construction.** Stat constructors are `internal`; use `StatFactory` with a `Request` object. This keeps the four stat variants consistent.
- **Four base variants** (combination of two axes):

  | | Static (stored raw) | Dynamic (computed by delegate) |
  |---|---|---|
  | **Plain** | `Stat` | `DynamicStat` |
  | **Modifiable** | `ModifiableStat` | `ModifiableDynamicStat` |

- **Evaluation order.** `Value = Clamp(Modifier(raw), LowerBoundary, UpperBoundary)`. Modifier formula: `(raw + Σ flat) × (1 + Σ percentage)`. Only sources with `EnableModification = true` contribute.
- **Reactive.** Every stat raises `OnValueChanged` when its effective value changes (compared with `RawValue.ApproxEquals`). Modifier sources notify their owner stat through `OnSourceChanged`, so a source stat (e.g. an equipment bonus) changing propagates automatically.
- **Stats as modifier sources.** `SourceStatPack` wraps any `IStat` as an `IStatModifySource`, so stats can modify other stats; `DestroyPack()` unsubscribes.
- **Floating-point tolerance.** `RawValue` is a `readonly struct` over `double` with `EPSILON = 1e-7`, tolerant comparisons, clamping, rounding and `int`/`long` conversions; implicit to `double`, explicit from `double`.
- **Composition via decorators.** `StatBoolLevel` and `StatResources` implement `IStat` on top of other stats.
- **Persistence flag.** `StatModifyInfo.Persistent` lets `ClearAllSources(clearPersistent: false)` keep permanent modifiers while clearing temporary ones.

## Features Breakdown

| Component | Description |
|---|---|
| `RawValue` | Value type with arithmetic, comparison, `Clamp/AtLeast/AtMost`, `IsZero/IsPositive/IsNegative`, `Lerp`, `AsInt*`, `AsLong*`. |
| `Stat` | Stored value; `SetValue(RawValue)`. |
| `DynamicStat` | Value from `Func<object?, RawValue>`; `SetValueFunc`, `GetValue(object?)`. |
| `ModifiableStat` / `ModifiableDynamicStat` | Add a `StatModifier`. |
| `StatModifier` | `AddSource`, `AddUniqueSource`, `RemoveSource`, `ClearAllSources`, `ClearDisabledSource`, `GetModifiedValue`. |
| `IStatModifySource`, `SourceStatPack`, `StatModifyInfo` | Modifier source contract and the stat-backed implementation. |
| `StatInfo` | `Name`, `Id`, `Remark`. |
| `StatBoolLevel` | View exposing `Success` (value positive) and `Level` (value as int). |
| `StatResources` | Resource pool (e.g. HP/MP) with `Current`, `Min`, `Max`, `IsFull`, `Deficit`, `Recover()`. |
| `StatFactory` | `CreateBaseStat`, `CreateBoolLevelStat`, `CreateResourcesStat`, `CreateSourceStatPack`, and the `Request` / `Request.Modify` descriptors. |
| `StatSet` | List-based container: lookup by id/name/index, typed `GetStat<T>`, `Add`, `AddUnique`, `Remove*`, `Clear`. |

## Usage Guide

### Create a modifiable stat and apply bonuses

```csharp
using EverbloomingLab.KometoroSDK.Game;

var attack = (ModifiableStat)StatFactory.CreateBaseStat(new StatFactory.Request
{
	Name = "Attack", Id = 1, RawValue = 100,
	LowerBoundary = 0, UpperBoundary = 9999,
	Modifiable = true,
});
attack.OnValueChanged += s => Console.WriteLine($"Attack = {s!.Value}");

// +20 flat from a sword
var sword = StatFactory.CreateSourceStatPack(
	new StatFactory.Request { Name = "SwordBonus", Id = 100, RawValue = 20 },
	new StatFactory.Request.Modify { EnableModification = true, PercentageModify = false, Persistent = false });

// +50% from a buff
var buff = StatFactory.CreateSourceStatPack(
	new StatFactory.Request { Name = "Rage", Id = 101, RawValue = 0.5 },
	new StatFactory.Request.Modify { EnableModification = true, PercentageModify = true, Persistent = false });

attack.Modifier.AddSource(sword);
attack.Modifier.AddSource(buff);
// (100 + 20) * (1 + 0.5) = 180
```

### Dynamic stat

```csharp
var speed = StatFactory.CreateBaseStat(new StatFactory.Request(dynamicMode: true)
{
	Name = "Speed",
	OnRawValueFunc = _ => (RawValue)(baseSpeed * terrainFactor),
});
```

### Resources and containers

```csharp
var hp = StatFactory.CreateResourcesStat(new StatFactory.Request { Name = "Hp", Id = 10 });
hp.Max = StatFactory.CreateBaseStat(new StatFactory.Request { Name = "HpMax", RawValue = 200 });

var set = new StatSet();
set.AddUnique(attack);
var found = set.GetStat<ModifiableStat>("Attack");
```

> Notes: `StatResources.UpperBoundary` / `LowerBoundary` setters throw `NotSupportedException` (they mirror `Max` / `Min`). Resources created by `CreateResourcesStat(Request)` need their `Current` / `Max` / `Min` values assigned by the caller. `StatFactory.CreateBaseStat` for non-dynamic stats casts `request.RawValue` (double) to `RawValue`.
