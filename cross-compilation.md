# Cross-compilation

This document demonstrates how to cross-compile .NET apps as container images. It is part of a [container workshop](README.md), which details fundamental workflows for using .NET in containers. 

The following patterns rely on a combination of [OCI image publishing](https://learn.microsoft.com/dotnet/core/docker/publish-as-container) and [Dockerfile](dockerfile-samples.md) patterns.

We have a set of useful samples at [dotnet/dotnet-docker](https://github.com/dotnet/dotnet-docker). They will be used in the remainder of this document, assuming that the repo has been cloned locally.

## Using `--platform` switch

`docker build` enables building images for other architectures. It assumes that QEMU is installed. We use a pattern that avoids .NET running emulated, which makes builds faster and more reliable.

Reference: https://gist.github.com/richlander/70cde3f0176d36862af80c41722acd47

This Dockerfile demonstrates our pattern.

```bash
$ pwd
/home/rich/git/dotnet-docker/samples/dotnetapp
$ cat Dockerfile.chiseled 
# Learn about building .NET container images:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
ARG TARGETARCH
WORKDIR /source

# copy csproj and restore as distinct layers
COPY *.csproj .
RUN dotnet restore -a $TARGETARCH

# copy and publish app and libraries
COPY . .
RUN dotnet publish -a $TARGETARCH --no-restore -o /app


# final stage/image
FROM mcr.microsoft.com/dotnet/runtime:8.0-jammy-chiseled
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["./dotnetapp"]
```

This pattern warrants explanation. The tags referenced in the two `FROM` statements are both multi-arch tags. The SDK tag is being coerced via `--platform=$BUILDPLATFORM` to always run natively. The second `FROM` statement will always pull an image that matches the `$TARGETPLATFORM`, set or otherwise. Last, the SDK will always build an app for the `$TARGETARCH`, set or otherwise.

Given:

- `docker build -t app .`
- x64 host

In that case, `$BUILDPLATFORM`, `$TARGETPLATFORM`, and `$TARGETARCH` will all match and target/use x64/amd64.

Given:

- `docker build -t app --platform linux/arm64 .`
- x64 host

In that case:

- `$BUILDPLATFORM` == `linux/amd64`
- `$TARGETPLATFORM` == `linux/arm64`
- `$TARGETARCH` == `arm64`

We can try this.

```bash
$ docker build -f Dockerfile.chiseled -t dotnetapp --platform linux/arm64 .
$ docker inspect dotnetapp | grep Arch
        "Architecture": "arm64",
$ docker run --rm dotnetapp
WARNING: The requested image's platform (linux/arm64) does not match the detected host platform (linux/amd64/v3) and no specific platform was requested
exec ./dotnetapp: exec format error
```

This image can now be pushed to a registry and pulled onto an Arm64 machine and will work.

This pattern works equally well with `docker buildx build`. It supports building multi-arch images, like `--platform linux/arm64,linux/arm32,linux/amd64`. The Dockerfile above can be built with that pattern, for multiple platforms at once.

## Publish OCI and architecture targeting

The .NET SDK has its own platform targeting model, as demonstrated by `-a $TARGETARCH` in the previous Dockerfile. A similar pattern can be used with OCI publishing.

Add package to `dotnetapp`.

```bash
$ pwd
/home/rich/git/dotnet-docker/samples/dotnetapp
$ dotnet add package Microsoft.NET.Build.Containers --version 8.0.100
```

Publish app for Arm64 (on x64 machine).

```bash
$ dotnet publish /t:PublishContainer -a arm64
MSBuild version 17.8.0+6cdef4241 for .NET
  Determining projects to restore...
  Restored /home/rich/git/dotnet-docker/samples/dotnetapp/dotnetapp.csproj (in 171 ms).
  dotnetapp -> /home/rich/git/dotnet-docker/samples/dotnetapp/bin/Release/net8.0/linux-arm64/dotnetapp.dll
  dotnetapp -> /home/rich/git/dotnet-docker/samples/dotnetapp/bin/Release/net8.0/linux-arm64/publish/
  Building image 'dotnetapp' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/runtime:8.0'.
  Pushed image 'dotnetapp:latest' to local registry via 'docker'.
```

Inspect image.

```bash
$ docker inspect dotnetapp | grep Arch
        "Architecture": "arm64",
```

## Publish OCI and architecture targeting with native AOT

Cross-compilation is a bit harder with native AOT since the native tool chain needs to cross-compile and more components need to be installed to enable that. It is useful to rely on container images for that.

This approaach will built a native AOT container image from Arm64 on an x64 machine, using OCI publish.

```bash
$ pwd
/home/rich/git/dotnet-docker/samples/releasesapi
$ docker run --rm -it -v $(pwd):/source -w /source mcr.microsoft.com/dotnet/nightly/sdk:8.0-jammy-aot dotnet publish -a arm64 -o app -p PublishProfile=DefaultContainer -p ContainerArchiveOutputPath=image/hello-native-api.tar.gz
MSBuild version 17.8.3+195e7f5a3 for .NET
  Determining projects to restore...
  Restored /source/releasesapi.csproj (in 12.03 sec).
/usr/share/dotnet/sdk/8.0.100-rtm.23523.2/Current/SolutionFile/ImportAfter/Microsoft.NET.Sdk.Solution.targets(36,5): warning NETSDK1194: The "--output" option isn't supported when building a solution. Specifying a solution-level output path results in all projects copying outputs to the same directory, which can lead to inconsistent builds. [/source/releasesapi.sln]
  releasesapi -> /source/bin/Release/net8.0/linux-arm64/releasesapi.dll
  Generating native code
  releasesapi -> /source/app/
  Building image 'releasesapi' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/runtime-deps:8.0'.
  Pushed image 'releasesapi:latest' to local archive at '/source/image/hello-native-api.tar.gz'.
$ docker load --input image/hello-native-api.tar.gz 
9f42fce59581: Loading layer  13.91MB/13.91MB
The image releasesapi:latest already exists, renaming the old one with ID sha256:fbf760a2ae3beaf6bbb1b64ca15b2575e54862c201392ed568bbb4f1c22b63a3 to empty string
Loaded image: releasesapi:latest
$ docker inspect releasesapi | grep Arch
        "Architecture": "arm64",
```

The resulting image was saved to a a local path. It coule just as easily have been pushed to a container registry.
