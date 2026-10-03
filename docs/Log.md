# KometoroSDK.Log

A static, queue-backed logging facility with caller-info capture, multiple output modes and pluggable console sinks. Part of the KometoroRDK (Rapid Development Kit) by Everblooming Lab (Catmint Works).

> **Showcase Disclaimer** – This is a **read-only showcase** of internal tooling from **Everblooming Lab (Catmint Works)**, published under the MIT License to demonstrate our engineering. We provide **no technical support, no SLA, and do not accept pull requests**.

## Overview

- Assembly: `Log` (`netstandard2.1`)
- Namespace: `EverbloomingLab.KometoroSDK.Log`
- Main types: `LogControl` (static facade), `ILogService` / `LogServiceAdapter` (injectable wrapper), `ILogOut`, `LogTag`, `ELogMode`, `ELogMessagePresetType`.

## Architecture & Principles

- **Zero-config call sites.** `Print` uses `[CallerMemberName]`, `[CallerFilePath]`, `[CallerLineNumber]`, so every entry carries member / file / line automatically.
- **Non-blocking file output.** File lines are produced by the caller and pushed into a `BlockingCollection<string>`; a dedicated long-running task drains the queue and writes to a `StreamWriter` (UTF-8). `Stop()` completes the queue, waits briefly (300 ms), flushes and disposes.
- **Crash mode.** `Start(..., crashMode: true)` sets `AutoFlush = true` so lines survive abrupt termination, at the cost of throughput.
- **Console sink is a delegate.** `LogControl` never writes to the console itself; callers pass an `Action<string>` (`_OnLogConsole`), which makes it host-agnostic (Unity, CLI, tests). Unity builds (`UNITY_EDITOR || UNITY_STANDALONE`) additionally mirror critical internal errors to `UnityEngine.Debug`.
- **Cheap when off.** All `Print` overloads return immediately if logging has not been started; the `Func<T>` overload defers message construction.
- **Object-aware formatting.** Types implementing `ILogOut` supply their own log string; non-string `IEnumerable`s are printed element by element; `null` prints `null`.
- **Serial numbers.** Each entry gets a monotonically increasing number (`Interlocked.Increment`).
- **Dependency injection friendly.** `ILogService` / `LogServiceAdapter` wrap the static API for code that prefers an interface.

## Features Breakdown

### Output modes (`ELogMode`)

| Mode | Console callback | File |
|---|---|---|
| `Normal` | formatted | formatted |
| `ConsoleOnly` | formatted | – |
| `RawPassOnly` | raw message | – |
| `RawPassAndLog` | raw message | formatted |
| `Silence` | – | formatted |

### Tags
`LogTag` is an implicit wrapper over `string` or `ELogMessagePresetType` (`Debug, Launcher, Data, Asset, Manager, Event, Ui, PlayerControl, Sound, Animation, Other, ConsoleDisplay, Warning, Error`). Default tag is `Debug`.

### API summary
- `LogControl.Start(path, crashMode)`, `Start(path, fileName, onLogConsole, crashMode)`, `Start(path, fileName, mode, onLogConsole, crashMode)`, `Start(onLogConsole)` (console-only), `Stop()`.
- `LogControl.Print(object|string|struct|Func<T>, LogTag, ...)`.
- State: `_IsStarted`, `_LogFilePath`, `_LogMode`.
- Log file name: `{fileName}_{yyyyMMddHHmmss}.log` (default file name `KMTR_log`).

## Usage Guide

```csharp
using EverbloomingLab.KometoroSDK.Log;

LogControl.Start(
	logFilePath: "Logs",
	fileName:    "MyGame",
	mode:        ELogMode.Normal,
	onLogConsole: Console.WriteLine,
	crashMode:   false);

LogControl.Print("Game started", ELogMessagePresetType.Launcher);
LogControl.Print(new[] { 1, 2, 3 });                    // one entry per element
LogControl.Print(() => ExpensiveDump(), "Custom");      // lazily evaluated

public class Player : ILogOut
{
	public string? LogOut() => $"Player(hp={Hp})";
	public int Hp;
}
LogControl.Print(new Player { Hp = 10 });

LogControl.Stop();
```

Via interface:

```csharp
ILogService log = new LogServiceAdapter();
log.Print("hello", ELogMessagePresetType.Debug);
```

> Note: the target directory passed to `Start` must already exist.
