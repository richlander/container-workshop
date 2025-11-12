# Publishing apps as OCI images

This document demonstrates how to [publish .NET console apps as container images](https://learn.microsoft.com/dotnet/core/docker/publish-as-container). The [workflows for ASP.NET Core apps](aspnetcore.md) are largely the same. These instructions are part of a [container workshop](README.md), which details fundamental workflows for using .NET in containers.

Related:

- [docker build publishing](dockerfile-samples.md)
- [OCI image publishing property reference](publish-oci-properties.md).

## Hello dotnet

The easiest way to publish an app to a container image is with .NET SDK OCI image publish. Console apps require installing a NuGet package to use OCI publish. The NuGet package isn't required for ASP.NET Core apps.

Create the app.

```bash
$ dotnet new console -n hello-dotnet
$ cd hello-dotnet
$ dotnet run
Hello, World!
```

Change `Program.cs` (so that it prints the name of the operating system):

```csharp
using System.Runtime.InteropServices;

Console.WriteLine($"Hello, {RuntimeInformation.OSDescription} on {RuntimeInformation.OSArchitecture}!");
```

Re-run:

```bash
$ dotnet run
Hello, Ubuntu 24.04.3 LTS on X64!
```

Your operating system will likely be different.

Publish and run app image.

```bash
$ dotnet publish -t:PublishContainer
$ docker run --rm hello-dotnet
Hello, Ubuntu 24.04.3 LTS on X64!
```

Inspect the image:

```bash
$ docker images hello-dotnet
REPOSITORY     TAG       IMAGE ID       CREATED              SIZE
hello-dotnet   latest    53444c11ae48   About a minute ago   203MB
```

It is straightforward to create smaller images using [Alpine](./publish-alpine.md), [Ubuntu Chiseled](./publish-ubuntu-chiseled.md), and various [.NET SDK publishing options](./publish-options.md).

## Default image configuration

Summary:

- Ubuntu is used for default base images [starting with .NET 10](https://github.com/dotnet/core/discussions/10074).
- .NET 8+ images [come with a new user](https://devblogs.microsoft.com/dotnet/securing-containers-with-rootless/), app.
- `dotnet publish` sets the user to `app`.
- The user is set via UID, as explained in [Running non-root .NET containers with Kubernetes](https://devblogs.microsoft.com/dotnet/running-nonroot-kubernetes-with-dotnet/).

Images are configured to use a non-root user, following secure by default principles.

```bash
$ docker run --rm --entrypoint bash hello-dotnet -c "cat /etc/os-release | head -n 1"
PRETTY_NAME="Ubuntu 24.04.3 LTS"
$ docker run --rm --entrypoint bash hello-dotnet -c "whoami"
app
$ docker run --rm --entrypoint bash hello-dotnet -c "cat /etc/passwd | grep app"
app:x:1654:1654::/home/app:/bin/sh
$ docker inspect hello-dotnet | grep User
            "User": "1654",
```

`ContainerUser=root` can be used to configure images to use the `root` user.

Audit the image, with [anchore/syft](https://github.com/anchore/syft).

```bash
$ docker run --rm anchore/syft mcr.microsoft.com/dotnet/runtime:8.0 | grep dotnet | wc -l
1
$ docker run --rm anchore/syft mcr.microsoft.com/dotnet/runtime:8.0 | grep deb | wc -l
92
```

There are 1 .NET and 92 `.deb` packages/libraries installed.
