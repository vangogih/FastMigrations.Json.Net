<h1 align="center">
<img alt="logo" src="./FastMigrationIcon.png" height="200px" />
<br/>
FastMigrations.Json.Net
</h1>

<div align="center">

![](https://img.shields.io/badge/unity-2019.4+-000.svg)
[![NuGet Version](https://img.shields.io/nuget/v/FastMigrations.Json)](https://www.nuget.org/packages/FastMigrations.Json)
[![openupm](https://img.shields.io/npm/v/io.vangogih.fastmigrations?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/io.vangogih.fastmigrations/)

[![tests](https://github.com/vangogih/FastMigrations.Json.Net/actions/workflows/release.yaml/badge.svg)](https://github.com/vangogih/FastMigrations.Json.Net/actions/workflows/release.yaml)
[![](https://vangogih.github.io/FastMigrations.Json.Net/badge_linecoverage.svg)](https://vangogih.github.io/FastMigrations.Json.Net/)

</div>

Provides an efficient way to write json file migrations for `Unity` and `dotnet`:

- At least 5-10x times faster then [Migrations.Json.Net](https://github.com/Weingartner/Migrations.Json.Net/tree/master). See [Benchmarks](#benchmarks)
- Compatible with:
    - Unity 2019.3+ and IL2CPP backend
    - [Newtonsoft Json Unity Package 2.+](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@2.0/manual/index.html)
    - Newtonsoft.Json version 12.+
- Small code size: Few internal types and few .callvirt.
- Immutable: Thread safety and robustness.

## Table of contents
<!-- TOC -->
  * [Quickstart](#quickstart)
  * [How to contribute](#how-to-contribute)
  * [Installation](#installation)
    * [.NET NuGet](#net-nuget)
      * [Install via .NET CLI](#install-via-net-cli)
      * [Install manually with .csproj](#install-manually-with-csproj)
    * [Unity](#unity)
      * [Install via UPM (using Git URL)](#install-via-upm--using-git-url-)
      * [Install via OpenUPM](#install-via-openupm)
      * [Install manually (using .unitypackage)](#install-manually--using-unitypackage-)
  * [Problem](#problem)
  * [Solution](#solution)
    * [Implementation](#implementation)
  * [Features](#features)
    * [Inheritance](#inheritance)
    * [MigratorMissingMethodHandling.Ignore](#migratormissingmethodhandlingignore)
  * [Benchmarks](#benchmarks)
* [Contact](#contact)
<!-- TOC -->

## Quickstart

1. Install plugin
2. Check `FastMigrationsConverter` xml-doc
3. Cheers :beers:

## How to contribute

Just check [project](https://github.com/users/vangogih/projects/2/views/1) and assign task to yourself. Otherwise don't hesitate to [create an Issue ](https://github.com/vangogih/FastMigrations.Json.Net/issues/new)

## Installation

### .NET NuGet

Compatible with `.NET Standard 2.0`. Full compatibility matrix you will
find [here](https://www.nuget.org/packages/FastMigrations.Json/#supportedframeworks-body-tab)

#### Install via .NET CLI

- ```csharp
  dotnet add package FastMigrations.Json --version 1.0.2
  ```

#### Install manually with .csproj

1. Open project where you want to add this plugin
2. Add this line under `ItemGroup`

- ```csharp
  <ItemGroup>
    <PackageReference Include="FastMigrations.Json" Version="1.0.2" />
  </ItemGroup>
  ```

### Unity

Requires:
- **Unity 2019.4+**
- **[Newtonsoft Json Unity Package 2.0.2](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@2.0/manual/index.html)**

#### Install via UPM (using Git URL)

1. Navigate to your project's Packages folder and open the manifest.json file.
2. Add this line below the "dependencies":

- ```json title="Packages/manifest.json"
    "io.vangogih.fastmigrations": "https://github.com/vangogih/FastMigrations.Json.Net.git?path=FastMigrations.Unity/Assets/FastMigrations#1.0.2",
  ```

3. UPM should now install the package.

#### Install via OpenUPM

1. The package is available on the [openupm registry](https://openupm.com/packages/io.vangogih.fastmigrations/). It's recommended to install
   it via [openupm-cli](https://github.com/openupm/openupm-cli).
2. Execute the `openupm` command.

- ```
      openupm add io.vangogih.fastmigrations
    ```

#### Install manually (using .unitypackage)

1. Download the .unitypackage from [releases](https://github.com/vangogih/FastMigrations.Json.Net/releases) page.
2. Open FastMigrations.Json.Net.x.x.x.unitypackage

## Problem

Let's imagine you have a beautiful game released in Google Play or AppStore. In the game you save player data in format:

```json
{
  "softCurrency": 100,
  "hardCurrency": 10
}
```

In C# it will look like:

```csharp
public class PlayerData
{
    public int soft;
    public int hard;
}
```

And with the next release game designers come to you and ask to add new types of currencies into the game. 
And you decide to change the structure of `PlayerData` and aggregate all currencies as `Dictionary`.

```csharp
public class PlayerData
{
    public Dictionary<Currency, int> Wallet;
}
```

And now you have 2 problems:

1. All current users will lose their progress because `soft` and `hard`
   won't be deserialized into `Dictionary`
2. If your type to deserialize changed but name was the same
   you would
   get [JsonDeserializationException](https://www.newtonsoft.com/json/help/html/Properties_T_Newtonsoft_Json_JsonSerializationException.htm)

And if you want to save back compatibility with previous version you have to migrate your json file from the first version to N (you current version). 

Otherwise player from version v1.0.0 won't be compatible with vN.0.0. 

And this plugin effectively solves the problem.

## Solution

Implement algorithm how calls a chain of methods in a correct order according to current json file version. 

- Version 0 is default and can be ignored.
- If json has version 3 but current version is 10, plugin have to call methods from 4 to 10

### Implementation

1. Mark your class to migrate with attribute `Migratable`
```csharp
// [Migratable(0)] you don't have to implement Migrate_0. For simplicity you can think that all classes have version 0 as default
[Migratable(1)]
public class PlayerData
{
    public Dictionary<Currency, int> Wallet;
}
```
2. Implement method with signature `private/protected static JObject Migrate_1(JObject rawJson)`
```csharp
[Migratable(1)]
public class PlayerData
{
    public Dictionary<Currency, int> Wallet;

    private static JObject Migrate_1(JObject rawJson)
    {
        var oldSoftToken = rawJson["soft"];
        var oldHardToken = rawJson["hard"];
    
        var oldSoftValue = oldSoftToken.ToObject<int>();
        var oldHardValue = oldHardToken.ToObject<int>();
        
        var newWallet = new Dictionary<Currency, int>
        {
            {Currency.Soft, oldSoftValue},
            {Currency.Hard, oldHardValue}
        };
        
        rawJson.Remove("soft"); // bonus: we can remove old fields from json file
        rawJson.Remove("hard");
        
        rawJson.Add("Wallet", JToken.FromObject(newWallet));

        return rawJson;
    }
}
```
3. Add `FastMigrationsConverter` to `JsonSerializerSettings.Converters` or as an argument to `JsonConvert.SerializeObject/JsonConvert.Deserialize<T>`
```csharp
var jsonString = @"{
  ""softCurrency"": 100,
  ""hardCurrency"": 10
}";
var migrator = new FastMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
// For deserialization
var result = JsonConvert.DeserializeObject<PlayerData>(jsonString, migrator);
// For serialization
var result = JsonConvert.SerializeObject(jsonString, migrator);
```
4. Profit :beers:

## Features
For Unity to avoid `Migrate_` methods deletion `Migratable` attribute is inherited from `UnityEngine.Scripting.PreserveAttribute`. Import plugin directly and remove this 

### Inheritance
Inheritance for attributes is turned off. It means that you can mark parent and child class with attribute and ONLY methods on child will be called. See also [test case](https://github.com/vangogih/FastMigrations.Json.Net/blob/46681dfe41b0a3246431f1d9080eb8478c9c5a3d/FastMigrations.Unity/Assets/FastMigrations/Tests/EditorMode/FastMigrationConverterTests.cs#L176)

1. Mark your `Migrate_` method on parent as `protected`
2. Mark child class with `Migratable` attribute
3. Call from child's method parent method

Example:
```csharp
[Migratable(1)]
public class Parent
{
    protected static JObject Migrate_1(JObject jsonObj)
    {
        MethodCallHandler.RegisterMethodCall(typeof(ParentMock), nameof(Migrate_1));
        return jsonObj;
    }
}

[Migratable(2)]
public class ChildV2 : Parent
{
    private static JObject Migrate_1(JObject jsonObj)
    {
        jsonObj = ParentMock.Migrate_1(jsonObj);
        return jsonObj;
    }

    private static JObject Migrate_2(JObject jsonObj)
    {
        MethodCallHandler.RegisterMethodCall(typeof(ChildV10Mock), nameof(Migrate_2));
        return jsonObj;
    }
}
```

### MigratorMissingMethodHandling.Ignore
If you create `FastMigrationsConverter` with this argument it will ignore absence of `Migrate_N` methods.

**Warning:** Plugin iterates from current version to N and EVERY TIME try to find method `Migrate_`. If you skip 5 methods, `System.Type.GetMethod` will be called 5 times for nothing. It can drop performance dramatically. 
I recommend to increase version +1 and use `MigratorMissingMethodHandling.ThrowException` instead.

Example:
```csharp
var migrator1 = new FastMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException); // will throw MigrationException because there is no 3..9 methods
var migrator2 = new FastMigrationsConverterMock(MigratorMissingMethodHandling.Ignore); // will ignore absence of 3..9 methods
[Migratable(10)]
public class ChildV10
{
    private static JObject Migrate_1(JObject jsonObj)
    {
        return jsonObj;
    }

    private static JObject Migrate_2(JObject jsonObj)
    {
        return jsonObj;
    }

    private static JObject Migrate_10(JObject jsonObj)
    {
        return jsonObj;
    }
}
```

## Benchmarks

I took idea from unsupported plugin [Migrations.Json.Net](https://github.com/Weingartner/Migrations.Json.Net/tree/master) 
here is comparison. Code you will find [here](https://github.com/vangogih/FastMigrations.Json.Net/blob/master/FastMigrations.Benchmark/JsonMigratorsPerformanceTests.cs)

```text
BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3447/23H2)
AMD Ryzen 7 4800HS with Radeon Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.100-rc.2.23502.2
DefaultJob : .NET 5.0.17 (5.0.1722.21314), X64 RyuJIT AVX2
```

| Method                             |       Mean |    StdDev |                    Ratio | Allocated |              Alloc Ratio |
|------------------------------------|-----------:|----------:|-------------------------:|----------:|-------------------------:|
| Complex_Base_Deserialize           |   5,932 ns |  43.65 ns |                     1.00 |   3.42 KB |                     1.00 |
| Complex_Weingartner_Deserialize    | 107,878 ns | 482.62 ns |         *(before) 18.20* |  42.71 KB |         *(before) 12.48* |
| Complex_FastMigrations_Deserialize |  16,394 ns |  66.10 ns |  *(after **x6.5**) 2.77* |   9.24 KB | *(after **x4.62**) 2.70* |
|                                    |            |           |                          |           |                          |
| Complex_Base_Serialize             |   3,510 ns |  25.84 ns |                     1.00 |   1.94 KB |                     1.00 |
| Complex_Weingartner_Serialize      |  88,219 ns | 520.26 ns |         *(before) 25.13* |  34.63 KB |         *(before) 17.87* |
| Complex_FastMigrations_Serialize   |  12,947 ns |  28.68 ns | *(after **x6.81**) 3.69* |   8.19 KB | *(after **x4.22**) 4.23* |
|                                    |            |           |                          |           |                          |
| Simple_Base_Deserialize            |   1,319 ns |   6.49 ns |                     1.00 |   2.61 KB |                     1.00 |
| Simple_Weingartner_Deserialize     |  22,472 ns |  81.02 ns |        *(before)  17.02* |  10.86 KB |         *(before)  4.16* |
| Simple_FastMigrations_Deserialize  |   3,447 ns |  17.02 ns | *(after **x6.52**) 2.61* |   4.05 KB | *(after **x2.68**) 1.55* |
|                                    |            |           |                          |           |                          |
| Simple_Base_Serialize              |     785 ns |   3.63 ns |                     1.00 |   1.35 KB |                     1.00 |
| Simple_Weingartner_Serialize       |  16,380 ns |  95.49 ns |        *(before)  20.86* |   8.07 KB |         *(before)  5.97* |
| Simple_FastMigrations_Serialize    |   2,702 ns |  17.68 ns | *(after **x6.06**) 3.44* |   2.88 KB | *(after **x2.80**) 2.13* |

# Contact
- Author: Aleksei Kozorezov aka vangogih
- [Telegram blog (RUS)](https://t.me/+n5I3OOpd6-0zOTMy)