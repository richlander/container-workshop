# Publish Options

The SDK offers publishing options to optimize app size and performance. They can be used with and are integrated with SDK container publishing.

This is a mini-tutorial on `dotnet` container publishing capabilities and behavior.

## App

We'll start by creating an app.

```bash
$ dotnet new console -n hello-dotnet
$ cd hello-dotnet
$ dotnet run
Hello, World!
```

We can now containerize the app.

## Base behavior

```bash
$ dotnet publish -t:PublishContainer
$ docker images hello-dotnet
REPOSITORY     TAG       IMAGE ID       CREATED          SIZE
hello-dotnet   latest    c07474d86ae6   24 seconds ago   203MB
$ docker run --rm hello-dotnet
Hello, World!
Found 5 files in the app directory.
$ docker run --rm --entrypoint bash hello-dotnet -c "ls"
hello-dotnet
hello-dotnet.deps.json
hello-dotnet.dll
hello-dotnet.pdb
hello-dotnet.runtimeconfig.json
```

This app is a typical framework dependent app -- with 5 files -- that relies on the runtime that comes in the base image.

The missing information is which base image was used to build the image. The `--verbosity` (`-v` for short) argument can be used to learn that.

```bash
$ dotnet publish -t:PublishContainer -v d | grep "Building image"
         Building image 'hello-dotnet' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/runtime:10.0'.
```

We can see that `dotnet/runtime:10.0` was chosen. This is the default runtime image. The app is a console app with no additional customization, making this  image a good choice.

## PublishSelfContained

The `PublishSelfContained` property changes the deployment model for the app.

```bash
$ grep Publish hello-dotnet.csproj 
    <PublishSelfContained>true</PublishSelfContained>
$ dotnet publish -t:PublishContainer -v d | grep "Building image"
         Building image 'hello-dotnet' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/runtime-deps:10.0'.
$ docker images hello-dotnet
REPOSITORY     TAG       IMAGE ID       CREATED         SIZE
hello-dotnet   latest    dbb7ec53bbab   6 seconds ago   203MB
$ docker run --rm hello-dotnet
Hello, World!
Found 192 files in the app directory.
```

The addition of `PublishSelfContained` changes the image chosen to `dotnet/runtime-deps:10.0`. The image size didn't change because all the same components are included. However, we now see all the runtime files in app directory, 192 minus the 5 we saw earlier.

## PublishTrimmed

The `PublishTrimmed` property optimizes the size of the app.

```bash
$ grep Publish hello-dotnet.csproj 
    <PublishSelfContained>true</PublishSelfContained>
    <PublishTrimmed>true</PublishTrimmed>
$ dotnet publish -t:PublishContainer -v d | grep "Building image"
         Building image 'hello-dotnet' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled-extra'.
$ docker images hello-dotnet
REPOSITORY     TAG       IMAGE ID       CREATED         SIZE
hello-dotnet   latest    3c0b89e9592c   2 minutes ago   76.3MB
$  docker run --rm hello-dotnet
Hello, World!
Found 30 files in the app directory.
```

The size of the image has decreased significantly and so has the file count. The remaining files are a mix of libraries that the app depends on.

## PublishSingleFile

The `PublishSingleFile` property packages the app as a single file.

```bash
$ grep Publish hello-dotnet.csproj 
    <PublishSelfContained>true</PublishSelfContained>
    <PublishTrimmed>true</PublishTrimmed>
    <PublishSingleFile>true</PublishSingleFile>
$ dotnet publish -t:PublishContainer -v d | grep "Building image"
         Building image 'hello-dotnet' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled-extra'.    
$ docker images hello-dotnet
REPOSITORY     TAG       IMAGE ID       CREATED          SIZE
hello-dotnet   latest    23c73c293dec   33 seconds ago   67.2MB
$ docker run --rm hello-dotnet
Hello World!
Found 2 files in the app directory.
```

The addition of `PublishSingleFile` reduces the size of the image because "managed code" `.dll` files are compressed. The file count is reduced to 2, the single-file app and symbols (`.pdb` file). `.pdb` files produced by the SDK are typically small.

## InvariantGlobalization

The [`InvariantGlobalization` property](https://learn.microsoft.com/dotnet/core/compatibility/globalization/6.0/culture-creation-invariant-mode) removes the dependency on the ICU globalization library (that comes with the base image). The library is the largest Linux archive package dependency for .NET apps, so removing it is substational. Apps need ICU to process non-ASCII text, currency symbols, calendar data, or other content related to human culture. Globalization functionality is demonstrated in [Publish container images with Dockerfile](./dockerfile-samples.md).

```bash
$ grep Publish hello-dotnet.csproj 
    <PublishSelfContained>true</PublishSelfContained>
    <PublishTrimmed>true</PublishTrimmed>
    <PublishSingleFile>true</PublishSingleFile>
$ grep Globalization hello-dotnet.csproj 
    <InvariantGlobalization>true</InvariantGlobalization>
$ dotnet publish -t:PublishContainer -v d | grep "Building image"
         Building image 'hello-dotnet' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled'.
$ docker images hello-dotnet
REPOSITORY     TAG       IMAGE ID       CREATED         SIZE
hello-dotnet   latest    355e686d7321   5 seconds ago   29MB
$ docker run --rm hello-dotnet
Hello World!
Found 2 files in the app directory.
```

The image is now much smaller. It is because the image has been changed from `10.0-noble-chiseled-extra` to `10.0-noble-chiseled`. The difference is that the former image contains `libicu` and `tzdata` and the latter doesn't.

## PublishAot

The [`PublishAot` property](https://learn.microsoft.com/dotnet/core/deploying/native-aot/) compiles the app as native code. Up until this point, we've seen progressive refinement of the published app. These all rely on the CoreCLR runtime. The final `PublishSingleFile` option produces the most optimized result for CoreCLR. `PublishAot` is a different family of solution using a different toolchain and results. It's enabled by a subtlety different property, but markedly different solution than the options that came to this point. This is also why the other properties have been removed in the example below.

```bash
$ grep Publish hello-dotnet.csproj 
    <PublishAot>true</PublishAot>
$ grep Globalization hello-dotnet.csproj 
    <InvariantGlobalization>true</InvariantGlobalization>
$ dotnet publish -t:PublishContainer -v d | grep "Building image"
         Building image 'hello-dotnet' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled'
$ docker images hello-dotnet
REPOSITORY     TAG       IMAGE ID       CREATED         SIZE
hello-dotnet   latest    be20161f30e2   9 seconds ago   17.9MB
$ docker run --rm hello-dotnet
Hello World!
Found 2 files in the app directory.
```

The image is now much smaller. Same base image. The app is now much smaller, in part because Native AOT is optimized to work well with `PublishTrimmed`.

We can make two more changes to further reduce image size. Like `PublishSingleFile`, the `PublishAot` publishing option produces an executable and a symbol file. However, `PublishAot` symbols are much larger. There is also a way to configure Native AOT to optize compilation for speed or size. We'll optimize it for size.

```bash
$ cat hello-dotnet.csproj 
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>hello_dotnet</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <PublishAot>true</PublishAot>
    <CopyOutputSymbolsToPublishDirectory>false</CopyOutputSymbolsToPublishDirectory>
    <OptimizationPreference>Size</OptimizationPreference>
    <InvariantGlobalization>true</InvariantGlobalization>
  </PropertyGroup>

</Project>
$ dotnet publish -t:PublishContainer -v d | grep "Building image"
         Building image 'hello-dotnet' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled'.
$ docker images hello-dotnet
REPOSITORY     TAG       IMAGE ID       CREATED         SIZE
hello-dotnet   latest    9f165a0c37f5   3 seconds ago   15.4MB
$ docker run --rm hello-dotnet
Hello World!
Found 1 files in the app directory.
```

The app is smaller again, down to 15.4MB.

## Publishing to Alpine

The `ContainerFamily` property configures `dotnet publish` to pick a different .NET based image family than the default. It is relevant for options discussed in this document. It will be demonstrated with Native AOT.

```bash
$ grep Container hello-dotnet.csproj 
    <ContainerFamily>alpine</ContainerFamily>
$ dotnet publish -t:PublishContainer -v d | grep "Building image"
         Building image 'hello-dotnet' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine'.
$ docker images hello-dotnet
REPOSITORY     TAG       IMAGE ID       CREATED          SIZE
hello-dotnet   latest    aee3742f74ad   11 seconds ago   12.5MB
```

The app is smaller again, down to 12.5MB. The image is based on Alpine, as the message describes.
