<h1 align="center">
<img alt="logo" src="./FastMigrationIcon.png" height="200px" />
<br/>
FastMigrations.Json.Net
</h1>

<div align="center">

![](https://img.shields.io/badge/unity-2019.4+-000.svg)
[![NuGet Version](https://img.shields.io/nuget/v/FastMigrations.Json)](https://www.nuget.org/packages/FastMigrations.Json)

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

## Important
Plugin is stable and ready to use, but I'm working on NPM and NuGet distribution.
You can check [Projects](https://github.com/users/vangogih/projects/2/views/1) to check tasks statuses
