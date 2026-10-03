# KometoroSDK.Event

A minimal, type-based, static publish/subscribe event bus. Part of the KometoroRDK (Rapid Development Kit) by Everblooming Lab (Catmint Works).

> **Showcase Disclaimer** – This is a **read-only showcase** of internal tooling from **Everblooming Lab (Catmint Works)**, published under the MIT License to demonstrate our engineering. We provide **no technical support, no SLA, and do not accept pull requests**.

## Overview

- Assembly: `Event` (`netstandard2.1`)
- Namespace: `EverbloomingLab.KometoroSDK.Event`
- Entry point: static class `EventCenter`; diagnostics hooks in static class `Log`.

## Architecture & Principles

- **Events are plain types.** Subscribers register an `Action<T>` for a type `T`; there are no string keys or base event classes.
- **Polymorphic dispatch.** On `Trigger`, the runtime type's hierarchy (base classes up to, but excluding, `object`, plus all implemented interfaces) is resolved once and cached. Handlers registered for a base class or an interface are therefore also invoked.
- **Type-erased handlers.** Internally each delegate is wrapped in a private `Handler<T>` implementing `IHandler`, which allows a single `Dictionary<Type, List<IHandler>>` to store everything.
- **Decoupled diagnostics.** `EventCenter` has no logging dependency. It raises optional delegates on `Log` (`OnSubscribed`, `OnUnsubscribed`, `OnTriggered`, `OnClearSubscriptions`, `Print`) so the host can plug in any logger. `Log.Print` defaults to `Console.WriteLine`.
- **Not thread-safe.** State is static and unsynchronised; intended for single-threaded (e.g. game main-loop) use.
- Handlers are invoked in **reverse registration order** per type bucket (iteration goes from the last item to the first, which makes unsubscribing during dispatch safer).

## Features Breakdown

| API | Description |
|---|---|
| `Subscribe<T>(Action<T>)` | Register a handler for `T` (duplicates allowed). |
| `Unsubscribe<T>(Action<T>)` | Remove the most recently added matching handler (one). |
| `UnsubscribeAll<T>(Action<T>)` | Remove every matching registration. |
| `Trigger<T>(T eventData)` | Dispatch to handlers of the runtime type, its base types and its interfaces. |
| `ClearAll()` | Drop all subscriptions and the hierarchy cache. |
| `Log.*` hooks | Optional observability callbacks. |

## Usage Guide

```csharp
using EverbloomingLab.KometoroSDK.Event;

public interface IGameEvent { }
public class DamageEvent : IGameEvent { public int Amount; }

void OnDamage(DamageEvent e)   => Console.WriteLine($"Damage {e.Amount}");
void OnAnyEvent(IGameEvent e)  => Console.WriteLine("Some game event");

EventCenter.Subscribe<DamageEvent>(OnDamage);
EventCenter.Subscribe<IGameEvent>(OnAnyEvent);

EventCenter.Trigger(new DamageEvent { Amount = 10 }); // both handlers run

EventCenter.Unsubscribe<DamageEvent>(OnDamage);
EventCenter.ClearAll();
```

Plug in diagnostics:

```csharp
Log.Print = msg => myLogger.Info(msg);
Log.OnTriggered = (type, data) => myLogger.Debug($"Triggered {type.Name}");
```

> Note: `Log.OnSubscribed` etc. are plain public static delegate fields; assign them before use.
