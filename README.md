# KometoroSDK

**KometoroSDK** (Rapid Development Kit) is an internal C# development framework created by **Everblooming Lab** (a DBA of Catmint Works LLC). 

Targeting `.NET Standard 2.1`, this SDK provides high-performance, zero-allocation-conscious, and decoupled architectural tools for independent game development and robust engine design.

## ⚠️ Showcase Disclaimer (Read-Only Repository)

This repository operates in a **"Showcase / Window"** mode. It is a public mirror of our internal development environment, published to share our engineering practices with the broader developer community.

* **No Pull Requests:** We do not accept PRs. Active development occurs in our private pipeline.
* **No Technical Support:** This code is provided "as-is". We offer no SLA and do not guarantee responses to issues.
* **Forking Encouraged:** You are highly encouraged to fork this repository and adapt the code for your own projects under the MIT License.

For more details on how to interact with this repository, please read our [CONTRIBUTING.md](CONTRIBUTING.md).

## 📦 Core Modules

KometoroSDK is heavily modularized. You can extract and integrate individual modules as needed. Detailed documentation for each subsystem is linked below:

| Module                                                       | Description                                                  |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| [**Data.ReadOnlyDatabase (RODB)**](docs/Data.ReadOnlyDatabase.md) | An in-memory, immutable, column-oriented lookup database for game configuration data. Features zero-allocation row views and O(1) lookups via string intern pools. |
| [**Data.BinaryPack (BinPack)**](docs/Data.BinaryPack.md)     | A compact, self-describing binary serialization container format. Built-in stream layering for AES encryption, GZip compression, and SHA-256 integrity validation. |
| [**Event**](docs/Event.md)                                   | A minimal, type-based, static publish/subscribe event bus. Features polymorphic dispatch, type-erased handlers, and decoupled external logging hooks. |
| [**Game.Stat**](docs/Game.Stat.md)                           | A complete RPG-style stat architecture. Supports clamped numeric stats, dynamic/computed stats, and reactive modifier pipelines (flat & percentage bonuses). |
| [**Collections**](docs/Collections.md)                       | Allocation-conscious collection helpers designed for high-frequency game loops. Provides looped iteration steps and wrapping list navigators via `readonly struct` metadata. |
| [**Log**](docs/Log.md)                                       | A static, queue-backed logging facility. Captures caller-info automatically, utilizes non-blocking file output, and supports pluggable console sinks (e.g., Unity Debug integration). |

## ⚙️ Integration & Compatibility

* **Target Framework:** `.NET Standard 2.1`
* **Dependencies:** Zero external dependencies. Modules rely only on standard C# base class libraries.
* **Usage:** Drop the source files directly into your C# project or Unity `Assets` folder, or compile as independent `.dll` libraries. 

## 🏢 About Us

**Everblooming Lab** is the experimental engineering and toolchain division of **Catmint Works LLC**. We focus on building robust, scalable game architectures and rapid development utilities that power our interactive projects.

* **Website:** [catmintworks.com](https://catmintworks.com)
* **Contact:** lab@catmintworks.com / contact@catmintworks.com 

## 📄 License

Released under the [MIT License](LICENSE). 
© 2026 Catmint Works LLC. All rights reserved.
