# Publishing Alpine images

The [hello-dotnet app](./publish-oci.md) can be published for Alpine. The .NET SDK (naturally) knows a lot about the app you are trying to build, including whether you are targeting `linux` (glibc) or `linux-musl` (musl libc; Alpine). It will pick an Alpine-based image for you if you target `linux-musl`.

Project file:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>hello_dotnet</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>

    <!-- Publishing properties -->
    <EnableSdkContainerSupport>true</EnableSdkContainerSupport>
  </PropertyGroup>

</Project>
```

Publish and run app image.

```bash
$ dotnet publish -t:PublishContainer --os linux-musl
$ docker run --rm hello-dotnet
Hello, Alpine Linux v3.19 on X64!
```

Base image used: `mcr.microsoft.com/dotnet/runtime:8.0-alpine`

Inspect image.

```bash
$ docker images hello-dotnet
REPOSITORY     TAG       IMAGE ID       CREATED          SIZE
hello-dotnet   latest    22cd27942810   23 seconds ago   82.8MB
```

## Self-contained

[Self-contained publishing](./publish-options.md) can target images that include globalization dependencies and not.

Project file with `InvariantGlobalization` == `true`.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>hello_dotnet</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <InvariantGlobalization>true</InvariantGlobalization>

    <!-- Publishing properties -->
    <EnableSdkContainerSupport>true</EnableSdkContainerSupport>
    <PublishTrimmed>true</PublishTrimmed>
    <ContainerRepository>hello-dotnet-alpine-trimmed</ContainerRepository>
  </PropertyGroup>

</Project>
```

Publish and run app image.

```bash
$ dotnet publish /t:PublishContainer --os linux-musl
$ docker run --rm hello-dotnet-alpine-trimmed
Hello, Alpine Linux v3.19 on X64!
```

Inspect image.

```bash
$ docker images hello-dotnet-alpine-trimmed
REPOSITORY                    TAG       IMAGE ID       CREATED         SIZE
hello-dotnet-alpine-trimmed   latest    1a36008ea823   5 seconds ago   30.9MB
```

Project file with `InvariantGlobalization` == `false`.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>hello_dotnet</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <InvariantGlobalization>false</InvariantGlobalization>

    <!-- Publishing properties -->
    <EnableSdkContainerSupport>true</EnableSdkContainerSupport>
    <PublishTrimmed>true</PublishTrimmed>
    <ContainerFamily>alpine-extra</ContainerFamily>
    <ContainerRepository>hello-dotnet-alpine-trimmed</ContainerRepository>
  </PropertyGroup>

</Project>
```

Note: `ContainerFamily` is explicitly specified, however, `InvariantGlobalization` == `false` should make that unnecessary. The tracking issue for this is [dotnet/sdk#39315](https://github.com/dotnet/sdk/issues/39315).

Publish and run app image.

```bash
$ dotnet publish /t:PublishContainer --os linux-musl
$ docker run --rm hello-dotnet-alpine-trimmed
Hello, Alpine Linux v3.19 on X64!
```

Base image: `mcr.microsoft.com/dotnet/runtime-deps:8.0-alpine-extra`

Inspect image.

```bash
$ docker images hello-dotnet-alpine-trimmed
REPOSITORY                    TAG       IMAGE ID       CREATED         SIZE
hello-dotnet-alpine-trimmed   latest    e6a463f8b05d   3 seconds ago   67.4MB
```
