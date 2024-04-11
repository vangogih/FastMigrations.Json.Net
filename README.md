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

- At least 5-10x times faster then [Migrations.Json.Net](https://github.com/Weingartner/Migrations.Json.Net/tree/master). See Benchmarks
- Compatible with:
  - Unity 2019.3+ and IL2CPP backend
  - [Newtonsoft Json Unity Package 2.+](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@2.0/manual/index.html)
  - Newtonsoft.Json version 12.+
- Small code size: Few internal types and few .callvirt.
- Immutable: Thread safety and robustness.

## Installation

### .NET
Compatible with `.NET Standard 2.0`. Full compatibility matrix you will find [here](https://www.nuget.org/packages/FastMigrations.Json/#supportedframeworks-body-tab)

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

*Requires Unity 2019.4+*

#### Install via UPM (using Git URL)

1. Navigate to your project's Packages folder and open the manifest.json file.
2. Add this line below the "dependencies":
- ```json title="Packages/manifest.json"
    "io.vangogih.fastmigrations": "https://github.com/vangogih/FastMigrations.Json.Net.git?path=FastMigrations.Unity/Assets/FastMigrations#1.0.2",
  ```
3. UPM should now install the package.

#### Install via OpenUPM


1. The package is available on the [openupm registry](https://openupm.com/packages/io.vangogih.fastmigrations/). It's recommended to install it via [openupm-cli](https://github.com/openupm/openupm-cli).
2. Execute the openum command.
  - ```
      openupm add io.vangogih.fastmigrations
    ```

#### Install manually (using .unitypackage)

1. Download the .unitypackage from [releases](https://github.com/vangogih/FastMigrations.Json.Net/releases) page.
2. Open FastMigrations.Json.Net.x.x.x.unitypackage
