## .NET Container workshop

The workshop details the fundamental workflows for using .NET with containers. It includes a variety of modalities, such [OCI image publish](https://learn.microsoft.com/dotnet/core/docker/publish-as-container), Dockerfile, [cross-compilation](https://devblogs.microsoft.com/dotnet/improving-multiplatform-container-support/), and [chiseled containers](https://devblogs.microsoft.com/dotnet/dotnet-6-is-now-in-ubuntu-2204/#net-in-chiseled-ubuntu-containers). OCI publish is used as the default approach.

Instructions:

- [.NET Console apps](dotnet.md)
- [ASP.NET Core web apps](aspnetcore.md)
- [Cross-compilation](cross-compilation.md)
- [Publishing apps within an SDK container](publish-in-sdk-container.md)

## Environment

The instructions assume:

- Docker.
- .NET 8

The following environment was used for the examples.

```bash
$ dotnet --version
8.0.100-rc.2.23502.2
$ docker --version
Docker version 24.0.7, build afdd53b
$ cat /etc/os-release | head -n 1
PRETTY_NAME="Ubuntu 22.04.3 LTS"
$ uname -a
Linux vancouver 6.2.0-35-generic #35~22.04.1-Ubuntu SMP PREEMPT_DYNAMIC Fri Oct  6 10:23:26 UTC 2 x86_64 x86_64 x86_64 GNU/Linux
```
