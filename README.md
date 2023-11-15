## .NET 8 container workshop

The workshop details the fundamental workflows for using .NET with containers. It includes a variety of approaches and capabilities, such as [OCI image publish](https://learn.microsoft.com/dotnet/core/docker/publish-as-container), registry push, Dockerfile, [cross-compilation](https://devblogs.microsoft.com/dotnet/improving-multiplatform-container-support/), and [chiseled containers](https://devblogs.microsoft.com/dotnet/dotnet-6-is-now-in-ubuntu-2204/#net-in-chiseled-ubuntu-containers). OCI publish is used as the default approach.

Instructions:

- [Publish OCI images](publish-oci.md)
- [Publish OCI image publishing reference](publish-oci-reference.md)
- [ASP.NET Core web apps](aspnetcore.md)
- [Dockerfile samples](dockerfile-samples.md)
- [Publishing apps within an SDK container](publish-in-sdk-container.md)
- [Cross-compilation](cross-compilation.md)
- [Publishing to a registry](push-to-registry.md)
- [Controlling how your containers run](./super-sql-app/control-container-runtime.md)
- [Dynamically Adapting To Application Sizes](https://maoni0.medium.com/dynamically-adapting-to-application-sizes-2d72fcb6f1ea)

## Find this repo

If you are using this repo in a talk, use this QR code to help people find the repo.

<img width="311" alt="QR code to repo" src="https://github.com/richlander/container-workshop/assets/2608468/4067d47d-5ea3-460e-9062-0050c611ba53">

## Environment

The instructions assume:

- [Docker](https://docs.docker.com/engine/install/)
- [.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

The following environment was used for the examples.

```bash
$ dotnet --version
8.0.100
$ docker --version
Docker version 24.0.7, build afdd53b
$ cat /etc/os-release | head -n 1
PRETTY_NAME="Ubuntu 22.04.3 LTS"
$ uname -a
Linux vancouver 6.2.0-35-generic #35~22.04.1-Ubuntu SMP PREEMPT_DYNAMIC Fri Oct  6 10:23:26 UTC 2 x86_64 x86_64 x86_64 GNU/Linux
```
