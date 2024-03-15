# .NET 8 container workshop

The workshop details the fundamental workflows for using .NET with containers. It includes a variety of approaches and capabilities, such as [OCI image publish](https://learn.microsoft.com/dotnet/core/docker/publish-as-container), registry push, Dockerfile, [cross-compilation](https://devblogs.microsoft.com/dotnet/improving-multiplatform-container-support/), and [chiseled containers](https://devblogs.microsoft.com/dotnet/dotnet-6-is-now-in-ubuntu-2204/#net-in-chiseled-ubuntu-containers). OCI publish is used as the default approach.

It assumes the use of .NET SDK `8.0.200` or later.

Instructions:

- [Publish OCI images](publish-oci.md)
- [ASP.NET Core web apps](aspnetcore.md)
- [Target Alpine](./publish-alpine.md)
- [Target Ubuntu Chiseled](./publish-ubuntu-chiseled.md)
- [.NET SDK Publish Option](./publish-options.md)
- [Troubleshooting](./troubleshooting.md)

Advanced instructions:

- [Publish OCI image publishing reference](publish-oci-reference.md)
- [Dockerfile samples](dockerfile-samples.md)
- [Publishing apps within an SDK container](publish-in-sdk-container.md)
- [Cross-compilation](cross-compilation.md)
- [Publishing to a registry](push-to-registry.md)
- [Controlling how your containers run](./super-sql-app/control-container-runtime.md)
- [Dynamically Adapting To Application Sizes](https://maoni0.medium.com/dynamically-adapting-to-application-sizes-2d72fcb6f1ea)

## Find this repo

If you are using this repo in a talk, use this QR code to help people find the repo.

<img width="311" alt="QR code to repo" src="https://github.com/richlander/container-workshop/assets/2608468/4067d47d-5ea3-460e-9062-0050c611ba53" />

## Environment

The instructions assume:

- [Docker](https://docs.docker.com/engine/install/)
- [.NET SDK 8.0.200+](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

The following environment was used for the examples.

```bash
$ dotnet --version
8.0.201
$ docker --version
Docker version 24.0.5, build 24.0.5-0ubuntu1
$ uname -a
Linux mazama 6.5.0-21-generic #21-Ubuntu SMP PREEMPT_DYNAMIC Wed Feb  7 14:17:40 UTC 2024 x86_64 x86_64 x86_64 GNU/Linux
```
