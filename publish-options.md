# Publish Options

The SDK offers publishing options to optimize app size and performance. They can be used with and are integrated with SDK container publishing.

For these examples, all relevant properties with be included in the project file. They can just as easily be used from the CLI.

## Self-contained

The [hello-dotnet app](./publish-oci.md) can be published as self-contained. We'll use a [chiseled base image](./publish-ubuntu-chiseled.md) again and use `ContainerRepository` to name the image.

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
    <PublishTrimmed>true</PublishTrimmed>
    <PublishSelfContained>true</PublishSelfContained>
    <ContainerFamily>jammy-chiseled-extra</ContainerFamily>
    <ContainerRepository>hello-chiseled-trimmed</ContainerRepository>
  </PropertyGroup>

</Project>
```

Note: The `EnableSdkContainerSupport` property is only needed for console apps. Apps that use the `Microsoft.NET.Sdk.Web` SDK (see first line project file) have this property set automatically.

Publish and run the app image:

```bash
$ dotnet publish /t:PublishContainer
$ docker run --rm hello-chiseled-trimmed
Hello, Ubuntu 22.04.3 LTS!
```

Base image used: `mcr.microsoft.com/dotnet/runtime-deps:8.0-jammy-chiseled-extra`

Note: You can identify that the build is trimming the app with the following line.

```bash
Optimizing assemblies for size. This process might take a while.
```

Inspect the image.

```bash
$ docker images hello-chiseled-trimmed
REPOSITORY               TAG       IMAGE ID       CREATED          SIZE
hello-chiseled-trimmed   latest    d5e16881e864   16 seconds ago   73.7MB
```

This image is a lot smaller than the default case.

As a reminder, the following is the default.

```bash
$ docker images hello-dotnet
REPOSITORY     TAG       IMAGE ID       CREATED          SIZE
hello-dotnet   latest    4f9032c723d1   14 seconds ago   193MB
```

## Decrease size with `InvariantGlobalization` mode

The ICU dependendency can be removed using [globalization-invariant mode](https://learn.microsoft.com/dotnet/core/compatibility/globalization/6.0/culture-creation-invariant-mode). It is the largest dependency for most apps. Globalization is [important for many apps](https://github.com/dotnet/dotnet-docker/tree/main/samples/globalapp), so it is important to use this mode with care. ICU is not used in the `hello-dotnet` app, so invariant mode can be enabled and the dependency can be removed.

`InvariantGlobalization` mode enables `PublishTrimmed` to remove a few more library methods, modestly decreasing the size of the underlying .NET libraries.

Project file:

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
    <ContainerFamily>jammy-chiseled</ContainerFamily>
    <ContainerRepository>hello-chiseled-trimmed</ContainerRepository>
  </PropertyGroup>

</Project>
```

Note: The `jammy-chiseled` container family does not include ICU or `tzdata`. The `jammy-chiseled-extra` family used in the previous example contain the globalization libraries. The following commands demonstrate that.

```bash
$ docker run --rm anchore/syft mcr.microsoft.com/dotnet/runtime-deps:8.0-jammy-chiseled-extra
NAME             VERSION                   TYPE   
base-files       12ubuntu4.5               deb     
ca-certificates  20230311ubuntu0.22.04.1   deb     
libc6            2.35-0ubuntu3.6           deb     
libgcc-s1        12.3.0-1ubuntu1~22.04     deb     
libicu70         70.1-2                    deb     
libssl3          3.0.2-0ubuntu1.14         deb     
libstdc++6       12.3.0-1ubuntu1~22.04     deb     
tzdata           2023d-0ubuntu0.22.04      deb     
zlib1g           1:1.2.11.dfsg-2ubuntu9.2  deb
$ docker run --rm anchore/syft mcr.microsoft.com/dotnet/runtime-deps:8.0-jammy-chiseled
NAME             VERSION                   TYPE   
base-files       12ubuntu4.5               deb     
ca-certificates  20230311ubuntu0.22.04.1   deb     
libc6            2.35-0ubuntu3.6           deb     
libgcc-s1        12.3.0-1ubuntu1~22.04     deb     
libssl3          3.0.2-0ubuntu1.14         deb     
libstdc++6       12.3.0-1ubuntu1~22.04     deb     
zlib1g           1:1.2.11.dfsg-2ubuntu9.2  deb
```

Publish and run the app image:

```bash
$ dotnet publish /t:PublishContainer
$ docker run --rm  hello-chiseled-trimmed
Hello, Ubuntu 22.04.3 LTS on X64!
```

Base image used: `mcr.microsoft.com/dotnet/runtime-deps:8.0-jammy-chiseled`

Inspect the image:

```bash
$ docker images hello-chiseled-trimmed
REPOSITORY               TAG       IMAGE ID       CREATED         SIZE
hello-chiseled-trimmed   latest    d0962426a554   3 seconds ago   36MB
```

The image is now half the size, using `InvariantGlobalization`.

## Native AOT

[Native AOT](https://learn.microsoft.com/dotnet/core/deploying/native-aot) compiles C# to native code and uses a much smaller build of CoreCLR, resulting in even smaller image sizes. The native AOT templates use `InvariantGlobalization` by default, so the examples will assume that configuration.

The project file uses `PublishAot`.

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
    <PublishAot>true</PublishAot>
    <ContainerRepository>hello-aot</ContainerRepository>
  </PropertyGroup>

</Project>
```

Publish and run the app image:

```bash
$ dotnet publish /t:PublishContainer
$ docker run --rm  hello-aot
Hello, Ubuntu 22.04.3 LTS on X64!
```

Base image used: `mcr.microsoft.com/dotnet/nightly/runtime-deps:8.0-jammy-chiseled-aot`

Note: You can identify that the build is compiling with Native AOT with the following line.

```bash
Generating native code
```

Inspect the image:

```bash
$ docker images hello-aot
REPOSITORY   TAG       IMAGE ID       CREATED          SIZE
hello-aot    latest    f3a375ea965c   14 seconds ago   14.8MB
```

The image is (less than) half the size again.

The project file above is missing a `ContainerFamily` property, instead relying on automatic behavior.

With Native AOT, the following base images are automatically used:

- `InvariantGlobalization` == `true`: `mcr.microsoft.com/dotnet/nightly/runtime-deps:8.0-jammy-chiseled-aot`
- `InvariantGlobalization` == `false`: `mcr.microsoft.com/dotnet/nightly/runtime-deps:8.0-jammy-chiseled-extra`

### Native AOT dependencies

You will get this error if you don't have a native toolchain installed.

```bash
$ dotnet publish /t:PublishContainer
MSBuild version 17.8.0+6cdef4241 for .NET
  Determining projects to restore...
  Restored /home/rich/hello-dotnet/hello-dotnet.csproj (in 254 ms).
  hello-dotnet -> /home/rich/hello-dotnet/bin/Release/net8.0/linux-x64/hello-dotnet.dll
/home/rich/.nuget/packages/microsoft.dotnet.ilcompiler/8.0.0/build/Microsoft.NETCore.Native.Unix.targets(199,5): error : Platform linker ('clang' or 'gcc') not found in PATH. Ensure you have all the required prerequisites documented at https://aka.ms/nativeaot-prerequisites. [/home/rich/hello-dotnet/hello-dotnet.csproj]
```

You can install the following packages, per https://aka.ms/nativeaot-prerequisites.

On Ubuntu, the following command will install the required components.

```bash
sudo apt install -y clang zlib1g-dev
```

If you don't want to install them (or cannot because you are on Windows), another pattern is discussed in [Publish OCI image in SDK container](publish-in-sdk-container.md).
