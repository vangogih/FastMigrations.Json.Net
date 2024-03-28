<img alt="logo" src="https://github.com/vangogih/FastMigrations.Json.Net/assets/30757221/e5041259-3b11-4364-aeae-657b2551f3e8" height="128px" />

# FastMigrations.Json.Net

![](https://img.shields.io/badge/unity-2019.3+-000.svg)
[![dotnet-tests](https://github.com/vangogih/FastMigrations.Json.Net/actions/workflows/dotnet-tests.yaml/badge.svg)](https://github.com/vangogih/FastMigrations.Json.Net/actions/workflows/dotnet-tests.yaml)
[![unity-tests](https://github.com/vangogih/FastMigrations.Json.Net/actions/workflows/unity-tests.yaml/badge.svg)](https://github.com/vangogih/FastMigrations.Json.Net/actions/workflows/unity-tests.yaml)

Provides an efficient way to write json file migrations for `Unity` and `dotnet`

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
