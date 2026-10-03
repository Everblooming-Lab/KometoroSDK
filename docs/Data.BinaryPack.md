# KometoroSDK.Data.BinaryPack (BinPack)

A compact binary container format with optional AES encryption, GZip compression and SHA-256 integrity checking, for serializing heterogeneous objects into a single file. Part of the KometoroRDK (Rapid Development Kit) by Everblooming Lab (Catmint Works).

> **Showcase Disclaimer** – This is a **read-only showcase** of internal tooling from **Everblooming Lab (Catmint Works)**, published under the MIT License to demonstrate our engineering. We provide **no technical support, no SLA, and do not accept pull requests**.

## Overview

- Assembly: `Data.BinaryPack` (`netstandard2.1`)
- Namespace: `EverbloomingLab.KometoroSDK.Data.BinPack`
- Write side: `BinPackage`. Read side: `BinPackDecoder`. Stream layering: `BinPackStream`.

## Architecture & Principles

### File layout

```
[Header – plain]
  8 bytes  magic        0x89 'K' 'M' 'T' 'R' 'B' 'T' 'P'
  4 bytes  format ver   (Extensions._Version)
  4 bytes  checksum flag (1 = SHA-256 appended)
  4 bytes  config length + config blob
		   (pkg version, encryption mode, compression mode, compression level,
			created time, name, remark)
[Body – passes through BinPackStream: GZip (outer) over AES (inner)]
  BinStringBundle   type-name mapping table (max 255 distinct types)
  int32             object count
  per object:       byte mappingIdx | int32 length | payload bytes
[Footer – optional]
  32 bytes SHA-256 of everything before it
```

### Design notes

- **Self-describing, extensible objects.** Each object is stored with a type-name key (e.g. `bp_s`, `bp_sb`). The decoder looks the key up in a static factory dictionary; custom types register via `BinPackDecoder.AddBinPackObjCreateFunction(key, func)`.
- **Length-prefixed payloads.** A faulty or unknown object never corrupts the rest of the stream: the decoder reads exactly `length` bytes into a reusable buffer, parses from a `MemoryStream`, and substitutes `BinNullObject` if the type is unknown, the read is short, or the parser did not consume the whole payload.
- **Defensive decoding.** Magic and checksum are verified before any parsing; objects larger than `MAX_DECODER_BUFFER` (10 MB) are skipped; mapping indices are range-checked.
- **Stream layering.** `BinPackStream` composes `CryptoStream` and `GZipStream` over the base stream according to `EEncryptionMode` / `ECompressionMode`. Written packages store `Encrypt`/`Compress`; the decoder flips them to `Decrypt`/`Decompress`.
- **Synchronous & async decode.** `Decode()` and `DecodeAsync(callback)`; failures are reported through `DecodeSuccess`, `ErrorMessage`, `IsChecksumPassed`.
- **Interfaces split by capability.** `IBinPackEncodable`, `IBinPackDecodable<T>`, `IBinPackCodable<T>`.

> **Security note:** the default AES key/IV in `Extensions` are hard-coded **debug values**. Always supply your own `AES_Key` (16-byte key + 16-byte IV) for real use. The key is held in static fields.

## Features Breakdown

| Component | Description |
|---|---|
| `BinPackage` | Builder/writer: `Add(IBinPackEncodable)`, `IsChecksum`, `WriteToFile(path)`. Several constructors for name, version, encryption, compression, level, remark. |
| `BinPackDecoder` | Reader: `ReadBinaryFile(path[, immediatelyDecode])`, `Decode()`, `DecodeAsync()`, indexer / `Count` / enumeration, metadata (`PackageName`, `PackageRemark`, `PackageCreatedTime`, `Version`, modes). |
| `BinPackStream` | `Stream` wrapper applying AES + GZip; optional `AES_Key`. |
| `BinString`, `BinStringBundle` | Built-in codable string / string-list objects (`bp_s`, `bp_sb`). |
| `BinNullObject` | Placeholder for undecodable entries. |
| `Extensions` | `ReadDecimal`, `ReadDateTime`/`WriteDateTime`, `IList<T>` codable read/write, fast blittable `ICollection<T>`/`T[]` read/write (`unmanaged`, via `MemoryMarshal` + `ArrayPool`), `ToBinaryString`, `ReadExact`. |
| `AES_Key` | Holder for key and IV. |
| `Log` | Optional logging hook used by the decoder/writer. |

## Usage Guide

### Define a custom object

```csharp
using EverbloomingLab.KometoroSDK.Data.BinPack;

public class PlayerSave : IBinPackCodable<PlayerSave>
{
	public static readonly BinString TypeName = new BinString("player_save");
	public int Level;
	public string Name = "";

	public BinString BinPackObjectName => TypeName;

	public void BinaryWrite(BinaryWriter w) { w.Write(Level); w.Write(Name); }

	public static PlayerSave Read(BinaryReader r) =>
		new PlayerSave { Level = r.ReadInt32(), Name = r.ReadString() };

	PlayerSave IBinPackDecodable<PlayerSave>.BinaryRead(BinaryReader r) => Read(r);
}
```

### Write

```csharp
BinPackDecoder.AddBinPackObjCreateFunction("player_save", PlayerSave.Read); // decoder side registration

var pkg = new BinPackage("save", 1,
	BinPackStream.EEncryptionMode.None,
	BinPackStream.ECompressionMode.Compress,
	remark: "slot 1")
{
	IsChecksum = true,
};
pkg.Add(new PlayerSave { Level = 5, Name = "Kometoro" });
pkg.Add("hello".ToBinaryString());
pkg.WriteToFile("slot1.kmtr");
```

### Read

```csharp
var dec = BinPackDecoder.ReadBinaryFile("slot1.kmtr", immediatelyDecode: true);
if (!dec.DecodeSuccess) Console.WriteLine(dec.ErrorMessage);

for (var i = 0; i < dec.Count; i++)
{
	if (dec[i] is PlayerSave p) Console.WriteLine($"{p.Name} L{p.Level}");
}

await BinPackDecoder.ReadBinaryFile("slot1.kmtr").DecodeAsync(d => Console.WriteLine(d.Count));
```

> Notes: encryption in `BinPackage` uses the static default key unless the `BinPackStream` constructor with `AES_Key` is used by the caller; check the source before relying on encryption in production.
