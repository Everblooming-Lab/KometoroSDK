# KometoroSDK.Collections

Small, allocation-conscious collection helpers for looped / stepped list traversal. Part of the KometoroRDK (Rapid Development Kit) by Everblooming Lab (Catmint Works).

> **Showcase Disclaimer** – This is a **read-only showcase** of internal tooling from **Everblooming Lab (Catmint Works)**, published under the MIT License to demonstrate our engineering. We provide **no technical support, no SLA, and do not accept pull requests or issues-as-support**.

## Overview

- Assembly: `Collections` (`netstandard2.1`)
- Namespace: `EverbloomingLab.KometoroSDK.Collections`
- Purpose: iterate over an `IReadOnlyList<T>` multiple times (loops) while exposing rich progress metadata, and navigate a list with a cursor (next / previous, optionally wrapping).

## Architecture & Principles

- **Read-only inputs** – every API takes `IReadOnlyList<T>`; the list is never mutated.
- **Lazy iteration** – `GetLoopStep` is an iterator (`yield return`), no intermediate buffers.
- **Value-type metadata** – `LoopIterationInfo<T>` is a `readonly struct`, so enumerating produces no per-step heap allocation for the info object.
- **Index math is modular** – start indices are normalised with `((i % n) + n) % n`, so negative or oversized start indices are safe.

## Features Breakdown

| Component | Description |
|---|---|
| `Extensions.GetLoopStep<T>(list, targetLoopCount, startIdx = 0)` | Extension on `IReadOnlyList<T>?`. Yields `count * targetLoopCount` steps. Returns an empty sequence for `null`, empty lists, or `targetLoopCount <= 0`. |
| `LoopIterationInfo<T>` | Per-step snapshot: `Item`, `CurrentLoop`, `CurrentStep`, `TotalLoopStep`, `TotalLoopCount`, plus `NormalizedProgress` (0–1 across the whole run) and `NormalizedProgressInCurrentLoop` (0–1 inside the current round). |
| `ListNavigator<T>` | Cursor over a list. `Current`, `Next()`, `Previous()`, `Reset(startIdx)`, `Loopable` (wrap-around vs. clamp at the ends), and counters `CurrentIndex`, `TotalSteps`, `TotalStepsForward`. Also exposes `GetLoopIterationInfos(...)`. |

## Usage Guide

### Looped iteration with progress

```csharp
using EverbloomingLab.KometoroSDK.Collections;

IReadOnlyList<string> frames = new[] { "A", "B", "C" };

foreach (var step in frames.GetLoopStep(targetLoopCount: 2))
{
	// step.Item, step.CurrentLoop (0..1), step.CurrentStep (0..2)
	Console.WriteLine($"{step.Item} loop={step.CurrentLoop} progress={step.NormalizedProgress:P0}");
}
```

Start from an offset (wraps around the list):

```csharp
foreach (var step in frames.GetLoopStep(1, startIdx: 2)) { /* C, A, B */ }
```

### Navigating a list

```csharp
var nav = new ListNavigator<string>(frames, startIdx: 0) { Loopable = true };

var a = nav.Next();      // "B"
var b = nav.Previous();  // "A"
nav.Reset(startIdx: 1);  // cursor -> "B", counters cleared
```

With `Loopable = false` the cursor stops at the first/last element instead of wrapping.

> Note: `ListNavigator<T>` assumes a non-empty list (index normalisation divides by the list count).
