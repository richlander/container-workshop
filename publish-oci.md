# Publishing apps as OCI images

The easiest way to publish an app to a container image is with [.NET SDK OCI image publish](https://learn.microsoft.com/dotnet/core/docker/publish-as-container). This document demonstrates how to publish .NET console apps as container images. The [workflows for ASP.NET Core apps](aspnetcore.md) are largely the same.  These instructions are part of a [container workshop](README.md), which details fundamental workflows for using .NET in containers.

Related:

- [docker build publishing](dockerfile-samples.md)
- [OCI image publishing property reference](publish-oci-properties.md).

## Hello dotnet

The easiest way to publish an app to a container image is with .NET SDK OCI image publish. Console apps require installing a NuGet package to use OCI publish. The NuGet package isn't required for ASP.NET Core apps.

Create and run app.

```bash
$ dotnet new console -n hello-dotnet
$ cd hello-dotnet
$ dotnet run
Hello, World!
```

Add package

```bash
$ dotnet add package Microsoft.NET.Build.Containers --version 8.0.100
```

Change the program (so that it will print the name of the operating system):

```bash
$ cat << EOF > Program.cs
> using System.Runtime.InteropServices;
> 
> Console.WriteLine($"Hello, {RuntimeInformation.OSDescription} on {RuntimeInformation.OSArchitecture}!");
> EOF
$ cat Program.cs 
using System.Runtime.InteropServices;

Console.WriteLine($"Hello, {RuntimeInformation.OSDescription} on {RuntimeInformation.OSArchitecture}!");
```

Note: The heredoc pattern was use to change the program. Other approaches can be used.

Re-run:

```bash
$ dotnet run
Hello, Ubuntu 22.04.3 LTS!
```

Publish to OCI image and launch a container.

```bash
$ dotnet publish /t:PublishContainer
$ docker run --rm hello-dotnet
Hello, Debian GNU/Linux 12 (bookworm)!
```

Publish should look like:

```bash
$ dotnet publish /t:PublishContainer
MSBuild version 17.8.0+6cdef4241 for .NET
  Determining projects to restore...
  All projects are up-to-date for restore.
  hello-dotnet -> /home/rich/hello-dotnet/bin/Release/net8.0/hello-dotnet.dll
  hello-dotnet -> /home/rich/hello-dotnet/bin/Release/net8.0/publish/
  Building image 'hello-dotnet' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/runtime:8.0'.
  Pushed image 'hello-dotnet:latest' to local registry via 'docker'.
```

- Base image downloaded by the SDK: `mcr.microsoft.com/dotnet/runtime:8.0`
- App image pushed to local daemon: `hello-dotnet:latest`


Look at the image:

```bash
$ docker images hello-dotnet
REPOSITORY     TAG       IMAGE ID       CREATED         SIZE
hello-dotnet   latest    191a85bdfbc9   5 seconds ago   193MB
$ docker run --rm --entrypoint bash hello-dotnet -c "cat /etc/os-release | head -n 1"
PRETTY_NAME="Debian GNU/Linux 12 (bookworm)"
$ docker run --rm --entrypoint bash hello-dotnet -c "whoami"
app
$ docker run --rm --entrypoint bash hello-dotnet -c "cat /etc/passwd | grep app"
app:x:1654:1654::/home/app:/bin/sh
$ docker inspect hello-dotnet | grep User
            "User": "",
            "User": "1654",
```

- Debian is used for default base images -- like `8.0` -- for .NET.
- .NET 8+ images [come with a new user](https://devblogs.microsoft.com/dotnet/securing-containers-with-rootless/), app.
- For .NET 8+ images, OCI publish sets the user to `app`.
- The user is set via UID, as explained in [Running non-root .NET containers with Kubernetes](https://devblogs.microsoft.com/dotnet/running-nonroot-kubernetes-with-dotnet/).

Audit the image, with [anchore/syft](https://github.com/anchore/syft).

```bash
$ docker run --rm anchore/syft mcr.microsoft.com/dotnet/runtime:8.0 | grep dotnet | wc -l
168
$ docker run --rm anchore/syft mcr.microsoft.com/dotnet/runtime:8.0 | grep deb | wc -l
92
```

There are 168 .NET and 92 `.deb` packages/libraries installed. 

## Targeting Ubuntu chiseled images

The same app can be published with an additional property -- `ContainerFamily` -- to target Ubuntu chiseled images. Chiseled images include fewer components and are much smaller as a result.

```bash
$ dotnet publish /t:PublishContainer /p:ContainerFamily=jammy-chiseled /p:ContainerRepository=hello-chiseled
$ docker run --rm hello-chiseled
Hello, Ubuntu 22.04.3 LTS!
```

Publish should look like:

```bash
$ dotnet publish /t:PublishContainer /p:ContainerFamily=jammy-chiseled /p:ContainerRepository=hello-chiseled
MSBuild version 17.8.0+6cdef4241 for .NET
  Determining projects to restore...
  All projects are up-to-date for restore.
  hello-dotnet -> /home/rich/hello-dotnet/bin/Release/net8.0/hello-dotnet.dll
  hello-dotnet -> /home/rich/hello-dotnet/bin/Release/net8.0/publish/
  Building image 'hello-chiseled' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/runtime:8.0-jammy-chiseled'.
  Pushed image 'hello-chiseled:latest' to local registry via 'docker'.
```

- Base image downloaded by the SDK: `mcr.microsoft.com/dotnet/runtime:8.0-jammy-chiseled`
- App image pushed to local daemon: `hello-chiseled:latest`
- The image name was specified by the optional `ContainerRepository` property

Look at the image:

```bash
$ docker images hello-chiseled
REPOSITORY       TAG       IMAGE ID       CREATED              SIZE
hello-chiseled   latest    4f712de1b269   About a minute ago   85.4MB
$ docker run --rm --entrypoint bash hello-chiseled -c "cat /etc/os-release | head -n 1"
docker: Error response from daemon: failed to create task for container: failed to create shim task: OCI runtime create failed: runc create failed: unable to start container process: exec: "bash": executable file not found in $PATH: unknown.
```

Running the entrypoint fails because `bash` isn't in the chiseled images.

The image is significantly smaller.

Audit the image, with [anchore/syft](https://github.com/anchore/syft).

```bash
$ docker run --rm anchore/syft mcr.microsoft.com/dotnet/runtime:8.0-jammy-chiseled | grep dotnet | wc -l
168
$ docker run --rm anchore/syft mcr.microsoft.com/dotnet/runtime:8.0-jammy-chiseled | grep deb | wc -l
7
$ docker run --rm anchore/syft mcr.microsoft.com/dotnet/runtime:8.0-jammy-chiseled | grep deb
base-files                                         12ubuntu4.4               de     
ca-certificates                                    20230311ubuntu0.22.04.1   de     
libc6                                              2.35-0ubuntu3.4           de     
libgcc-s1                                          12.3.0-1ubuntu1~22.04     de     
libssl3                                            3.0.2-0ubuntu1.10         de     
libstdc++6                                         12.3.0-1ubuntu1~22.04     de     
zlib1g                                             1:1.2.11.dfsg-2ubuntu9.2  de
```

There are 168 .NET and 7 `.deb` packages/libraries installed. That's a dramatic reduction of Linux components.

## Targeting Alpine images

The same app can be publish for Alpine. The .NET SDK (naturally) knows a lot about the app you are trying to build, including whether you are targeting `linux` (glibc) or `linux-musl` (musl libc; Alpine). However, that [doesn't currently affect OCI publish](https://github.com/dotnet/sdk-container-builds/issues/301), but should. If you want to target Alpine, `ContainerFamily` must be used.

```bash
$ dotnet publish /t:PublishContainer --os linux-musl /p:ContainerFamily=alpine /p:ContainerRepository=hello-musl
$ docker run --rm hello-musl
Hello, Alpine Linux v3.18!
```

Publish should look like the following.

```bash
$ dotnet publish /t:PublishContainer --os linux-musl /p:ContainerFamily=alpine /p:ContainerRepository=hello-musl
MSBuild version 17.8.0+6cdef4241 for .NET
  Determining projects to restore...
  Restored /home/rich/hello-dotnet/hello-dotnet.csproj (in 237 ms).
  hello-dotnet -> /home/rich/hello-dotnet/bin/Release/net8.0/linux-musl-x64/hello-dotnet.dll
  hello-dotnet -> /home/rich/hello-dotnet/bin/Release/net8.0/linux-musl-x64/publish/
  Building image 'hello-musl' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/runtime:8.0-alpine'.
  Pushed image 'hello-musl:latest' to local registry via 'docker'.
```

The `mcr.microsoft.com/dotnet/runtime:8.0-alpine` base image was used.

```bash
$ docker images hello-musl
REPOSITORY   TAG       IMAGE ID       CREATED         SIZE
hello-musl   latest    30cab2de6a37   9 minutes ago   83MB
$ docker run --rm anchore/syft mcr.microsoft.com/dotnet/runtime:8.0-alpine | grep apk | wc -l
17
```

As expected, Alpine is significantly smaller than Debian and about the same size as Ubuntu Chiseled. It contains more components than chiseled images.

## Publishing self-contained

The same app can be published as self-contained. We'll use chiseled again.

```bash
$ dotnet publish /t:PublishContainer /p:ContainerFamily=jammy-chiseled /p:ContainerRepository=hello-chiseled-trimmed /p:PublishTrimmed=true --self-contained
$ docker images hello-chiseled-trimmed
REPOSITORY               TAG       IMAGE ID       CREATED         SIZE
hello-chiseled-trimmed   latest    8a166d24b139   6 seconds ago   33.6MB
$ docker run --rm hello-chiseled-trimmed
Hello, Ubuntu 22.04.3 LTS!
```

This image is a lot smaller. Publish should look like the following.

```bash
$ dotnet publish /t:PublishContainer /p:ContainerFamily=jammy-chiseled /p:ContainerRepository=hello-chiseled-trimmed /p:PublishTrimmed=true --self-contained
MSBuild version 17.8.0+6cdef4241 for .NET
  Determining projects to restore...
  Restored /home/rich/hello-dotnet/hello-dotnet.csproj (in 844 ms).
  hello-dotnet -> /home/rich/hello-dotnet/bin/Release/net8.0/linux-x64/hello-dotnet.dll
  Optimizing assemblies for size. This process might take a while.
  hello-dotnet -> /home/rich/hello-dotnet/bin/Release/net8.0/linux-x64/publish/
  Building image 'hello-chiseled-trimmed' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/runtime-deps:8.0-jammy-chiseled'.
  Pushed image 'hello-chiseled-trimmed:latest' to local registry via 'docker'.
```

`mcr.microsoft.com/dotnet/runtime-deps:8.0-jammy-chiseled` is used as the base image. It contains .NET runtime dependencies, only.

```bash
$ docker run --rm anchore/syft mcr.microsoft.com/dotnet/runtime-deps:8.0-jammy-chiseled
NAME             VERSION                   TYPE 
base-files       12ubuntu4.4               deb   
ca-certificates  20230311ubuntu0.22.04.1   deb   
libc6            2.35-0ubuntu3.4           deb   
libgcc-s1        12.3.0-1ubuntu1~22.04     deb   
libssl3          3.0.2-0ubuntu1.10         deb   
libstdc++6       12.3.0-1ubuntu1~22.04     deb   
zlib1g           1:1.2.11.dfsg-2ubuntu9.2  deb
$ docker run --rm anchore/syft mcr.microsoft.com/dotnet/runtime-deps:8.0-jammy-chiseled | grep deb | wc -l
7
```

## Moving properties to the project file

In general, it is best to persist desired publish properties to the project file to avoid the extra effort (and error proneness) of passing arguments via the CLI. In addition, analyzers provide useful diagnostic feedback (in Visual Studio) for `PublishTrimmed` when it is set in the project file.

Publishing properties are now in the project file.

```bash
$ cat hello-dotnet.csproj 
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>hello_dotnet</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>

    <!-- Publishing properties -->
    <PublishTrimmed>true</PublishTrimmed>
    <PublishSelfContained>true</PublishSelfContained>
    <ContainerFamily>jammy-chiseled</ContainerFamily>
    <ContainerRepository>hello-chiseled-trimmed</ContainerRepository>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Build.Containers" Version="8.0.100" />
  </ItemGroup>

</Project>
```

The following -- simpler -- publish invocation does the same thing as the previous longer one.

```bash
$ dotnet publish /t:PublishContainer
MSBuild version 17.8.0+6cdef4241 for .NET
  Determining projects to restore...
  Restored /home/rich/hello-dotnet/hello-dotnet.csproj (in 221 ms).
  hello-dotnet -> /home/rich/hello-dotnet/bin/Release/net8.0/linux-x64/hello-dotnet.dll
  Optimizing assemblies for size. This process might take a while.
  hello-dotnet -> /home/rich/hello-dotnet/bin/Release/net8.0/linux-x64/publish/
  Building image 'hello-chiseled-trimmed' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/runtime-deps:8.0-jammy-chiseled'.
  Pushed image 'hello-chiseled-trimmed:latest' to local registry via 'docker'.
```

## Publishing as native AOT

The same app can be published with native AOT. We'll use chiseled again.

The project file has been updated to include `PublishAot` and change the name of the image.

```bash
$ grep PublishAot hello-dotnet.csproj
    <PublishAot>true</PublishAot>
$ grep ContainerRepository hello-dotnet.csproj 
    <ContainerRepository>hello-chiseled-aot</ContainerRepository>
```

The app can then be published.

```bash
$ dotnet publish /t:PublishContainer
MSBuild version 17.8.0+6cdef4241 for .NET
  Determining projects to restore...
  Restored /home/rich/hello-dotnet/hello-dotnet.csproj (in 254 ms).
  hello-dotnet -> /home/rich/hello-dotnet/bin/Release/net8.0/linux-x64/hello-dotnet.dll
/home/rich/.nuget/packages/microsoft.dotnet.ilcompiler/8.0.0/build/Microsoft.NETCore.Native.Unix.targets(199,5): error : Platform linker ('clang' or 'gcc') not found in PATH. Ensure you have all the required prerequisites documented at https://aka.ms/nativeaot-prerequisites. [/home/rich/hello-dotnet/hello-dotnet.csproj]
```

You will get this error if you don't have a native toolchain installed.

You can install the following packges, per https://aka.ms/nativeaot-prerequisites.

On Ubuntu, the following command will install the required components.

```bash
$ sudo apt install -y clang zlib1g-dev
```

If you don't want to install them, another pattern is discussed in [Publish OCI image in SDK container](publish-in-sdk-container.md).

Publish should look like:

```bash
$ dotnet publish /t:PublishContainer
MSBuild version 17.8.0+6cdef4241 for .NET
  Determining projects to restore...
  All projects are up-to-date for restore.
  hello-dotnet -> /home/rich/hello-dotnet/bin/Release/net8.0/linux-x64/hello-dotnet.dll
  Generating native code
  hello-dotnet -> /home/rich/hello-dotnet/bin/Release/net8.0/linux-x64/publish/
  Building image 'hello-chiseled-aot' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/runtime-deps:8.0-jammy-chiseled'.
  Pushed image 'hello-chiseled-aot:latest' to local registry via 'docker'.
```

The `mcr.microsoft.com/dotnet/runtime-deps:8.0-jammy-chiseled` base image is used.

Notice `Generating native code`. That's specific to native AOT publishing.

The app can now be run.

```bash
$ docker run --rm hello-chiseled-aot
Hello, Ubuntu 22.04.3 LTS!
```

This app is even smaller and includes 8 components, the same as was seen for the self-contained publish (because the base image is the same one).

```bash
$ docker images hello-chiseled-aot
REPOSITORY           TAG       IMAGE ID       CREATED         SIZE
hello-chiseled-aot   latest    b6e41eefd53c   3 minutes ago   17.4MB
$ docker run --rm anchore/syft mcr.microsoft.com/dotnet/runtime-deps:8.0-jammy-chiseled
NAME             VERSION                   TYPE 
base-files       12ubuntu4.4               deb   
ca-certificates  20230311ubuntu0.22.04.1   deb   
libc6            2.35-0ubuntu3.4           deb   
libgcc-s1        12.3.0-1ubuntu1~22.04     deb   
libssl3          3.0.2-0ubuntu1.10         deb   
libstdc++6       12.3.0-1ubuntu1~22.04     deb   
zlib1g           1:1.2.11.dfsg-2ubuntu9.2  deb
$ docker run --rm anchore/syft mcr.microsoft.com/dotnet/runtime-deps:8.0-jammy-chiseled | wc -l
8
```

We can make the app image a bit smaller by using an experimental base image (which requires more configuration). Notice that it is in a `nightly` repo.

```bash
$ grep ContainerBaseImage hello-dotnet.csproj 
    <ContainerBaseImage>mcr.microsoft.com/dotnet/nightly/runtime-deps:8.0-jammy-chiseled-aot</ContainerBaseImage>
```

`ContainerBaseImage` is a sort of "manual mode" where the desired base image can be specified. The SDK will no longer use a default image based on your publishing choices.

```bash
$ dotnet publish /t:PublishContainer
MSBuild version 17.8.0+6cdef4241 for .NET
  Determining projects to restore...
  All projects are up-to-date for restore.
  hello-dotnet -> /home/rich/hello-dotnet/bin/Release/net8.0/linux-x64/hello-dotnet.dll
  Generating native code
  hello-dotnet -> /home/rich/hello-dotnet/bin/Release/net8.0/linux-x64/publish/
  Building image 'hello-chiseled-aot' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/nightly/runtime-deps:8.0-jammy-chiseled-aot'.
  Pushed image 'hello-chiseled-aot:latest' to local registry via 'docker'.
$ docker images hello-chiseled-aot
REPOSITORY           TAG       IMAGE ID       CREATED         SIZE
hello-chiseled-aot   latest    4d866101f112   5 seconds ago   15.1MB
```

That experimental base image dropped 2MB. That's because native AOT doesn't have a dependency on `libstdc++6` and it isn't included in the `jammy-chiseled-aot` image.

```bash
$ docker run --rm anchore/syft mcr.microsoft.com/dotnet/nightly/runtime-deps:8.0-jammy-chiseled-aot
NAME             VERSION                   TYPE 
base-files       12ubuntu4.4               deb   
ca-certificates  20230311ubuntu0.22.04.1   deb   
libc6            2.35-0ubuntu3.4           deb   
libgcc-s1        12.3.0-1ubuntu1~22.04     deb   
libssl3          3.0.2-0ubuntu1.10         deb   
zlib1g           1:1.2.11.dfsg-2ubuntu9.2  deb
$ docker run --rm anchore/syft mcr.microsoft.com/dotnet/nightly/runtime-deps:8.0-jammy-chiseled-aot | wc -l
7
```
