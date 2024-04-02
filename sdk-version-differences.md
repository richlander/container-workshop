# .NET SDK version differences

The workshop instructions are written for [.NET SDK 8.0.200+](https://github.com/dotnet/sdk/blob/main/src/Containers/docs/ReleaseNotes/v8.0.200.md). 8.0.1xx users must use a slightly different pattern. The key differences are described in this document with the goal of making the workshop equally useful and straightforward to use.

## Console apps

[dotnetapp](https://github.com/dotnet/dotnet-docker/tree/main/samples/dotnetapp) is used as as the sample.

With `8.0.1xx`, the [Microsoft.NET.Build.Containers](https://www.nuget.org/packages/Microsoft.NET.Build.Containers) package must be added to publish an app, 


```bash
$ dotnet --version
8.0.102
$ dotnet add package Microsoft.NET.Build.Containers
$ dotnet publish -t:PublishContainer
/home/rich/.nuget/packages/microsoft.net.build.containers/8.0.202/build/Microsoft.NET.Build.Containers.targets(194,5): warning : Microsoft.NET.Build.Containers NuGet package is explicitly referenced. Consider removing the package reference to Microsoft.NET.Build.Containers as it is now part of .NET SDK. [/home/rich/git/dotnet-docker/samples/dotnetapp/dotnetapp.csproj]
  dotnetapp -> /home/rich/git/dotnet-docker/samples/dotnetapp/bin/Release/net8.0/publish/
  Building image 'dotnetapp' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/runtime:8.0'.
  Pushed image 'dotnetapp:latest' to local registry via 'docker'.
$ docker run --rm dotnetapp
         42
         42              ,d                             ,d
         42              42                             42
 ,adPPYb,42  ,adPPYba, MM42MMM 8b,dPPYba,   ,adPPYba, MM42MMM
a8"    `Y42 a8"     "8a  42    42P'   `"8a a8P_____42   42
8b       42 8b       d8  42    42       42 8PP!!!!!!!   42
"8a,   ,d42 "8a,   ,a8"  42,   42       42 "8b,   ,aa   42,
 `"8bbdP"Y8  `"YbbdP"'   "Y428 42       42  `"Ybbd8"'   "Y428

OSArchitecture: X64
OSDescription: Debian GNU/Linux 12 (bookworm)
FrameworkDescription: .NET 8.0.3

UserName: app
HostName : ce494dc0a437

ProcessorCount: 8
TotalAvailableMemoryBytes: 67382329344 (62.75 GiB)
```

Tracking issue on warning: [dotnet/sdk #39931](https://github.com/dotnet/sdk/issues/39931).

With `8.0.2xx`, the package does not need to be installed, but does require that `EnableSdkContainerSupport` is set.

```bash
$ dotnet --version
8.0.203
$ dotnet publish -t:PublishContainer -p:EnableSdkContainerSupport=true
  Building image 'dotnetapp' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/runtime:8.0'.
  Pushed image 'dotnetapp:latest' to local registry via 'docker'.
$ docker run --rm dotnetapp
         42
         42              ,d                             ,d
         42              42                             42
 ,adPPYb,42  ,adPPYba, MM42MMM 8b,dPPYba,   ,adPPYba, MM42MMM
a8"    `Y42 a8"     "8a  42    42P'   `"8a a8P_____42   42
8b       42 8b       d8  42    42       42 8PP!!!!!!!   42
"8a,   ,d42 "8a,   ,a8"  42,   42       42 "8b,   ,aa   42,
 `"8bbdP"Y8  `"YbbdP"'   "Y428 42       42  `"Ybbd8"'   "Y428

OSArchitecture: X64
OSDescription: Debian GNU/Linux 12 (bookworm)
FrameworkDescription: .NET 8.0.3

UserName: app
HostName : 0b7e69a77d97

ProcessorCount: 8
TotalAvailableMemoryBytes: 67382329344 (62.75 GiB)
```

## ASP.NET Core apps

There is no difference for ASP.NET Core apps. This pattern works for both SDK versions and produces the same results.

```bash
$ dotnet --version
8.0.102
$ dotnet publish -t:PublishContainer
MSBuild version 17.8.5+b5265ef37 for .NET
  Determining projects to restore...
  Restored /home/rich/git/dotnet-docker/samples/aspnetapp/aspnetapp/aspnetapp.csproj (in 125 ms).
  aspnetapp -> /home/rich/git/dotnet-docker/samples/aspnetapp/aspnetapp/bin/Release/net8.0/aspnetapp.dll
  aspnetapp -> /home/rich/git/dotnet-docker/samples/aspnetapp/aspnetapp/bin/Release/net8.0/publish/
  Building image 'aspnetapp' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/aspnet:8.0'.
  Pushed image 'aspnetapp:latest' to local registry via 'docker'.
```

## Base image inferencing

.NET SDK 8.0.200 and the `Microsoft.NET.Build.Containers` versions `8.0.200`+ includes some additional inferencing logic, for example to target Alpine. Earlier SDKs require more manual operations.


With the `8.0.1xx` using the `8.0.1xx` package, `ContainerFamily` must be used to target Alpine.

```bash
$ dotnet --version
8.0.102
$ cat dotnetapp.csproj | grep Package
    <PackageReference Include="Microsoft.NET.Build.Containers" Version="8.0.103" />
$ dotnet publish --os linux-musl -t:PublishContainer
  Building image 'dotnetapp' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/runtime:8.0'.
  Pushed image 'dotnetapp:latest' to local registry via 'docker'.
$ dotnet publish --os linux-musl -t:PublishContainer -p:ContainerFamily=alpine
  Building image 'dotnetapp' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/runtime:8.0-alpine'.
  Pushed image 'dotnetapp:latest' to local registry via 'docker'.
$ docker run --rm dotnetapp
         42
         42              ,d                             ,d
         42              42                             42
 ,adPPYb,42  ,adPPYba, MM42MMM 8b,dPPYba,   ,adPPYba, MM42MMM
a8"    `Y42 a8"     "8a  42    42P'   `"8a a8P_____42   42
8b       42 8b       d8  42    42       42 8PP!!!!!!!   42
"8a,   ,d42 "8a,   ,a8"  42,   42       42 "8b,   ,aa   42,
 `"8bbdP"Y8  `"YbbdP"'   "Y428 42       42  `"Ybbd8"'   "Y428

OSArchitecture: X64
OSDescription: Alpine Linux v3.19
FrameworkDescription: .NET 8.0.3

UserName: app
HostName : 8d19c0223a1b

ProcessorCount: 8
TotalAvailableMemoryBytes: 67382329344 (62.75 GiB)
```

With the `8.0.1xx` using the `8.0.2xx` package, automatic inferencing selects the correct base image.

```bash
rich@mazama:~/git/dotnet-docker/samples/dotnetapp$ dotnet --version
8.0.102
rich@mazama:~/git/dotnet-docker/samples/dotnetapp$ cat dotnetapp.csproj | grep Package
    <PackageReference Include="Microsoft.NET.Build.Containers" Version="8.0.202" />
rich@mazama:~/git/dotnet-docker/samples/dotnetapp$ dotnet publish --os linux-musl -t:PublishContainer
/home/rich/.nuget/packages/microsoft.net.build.containers/8.0.202/build/Microsoft.NET.Build.Containers.targets(194,5): warning : Microsoft.NET.Build.Containers NuGet package is explicitly referenced. Consider removing the package reference to Microsoft.NET.Build.Containers as it is now part of .NET SDK. [/home/rich/git/dotnet-docker/samples/dotnetapp/dotnetapp.csproj]
  dotnetapp -> /home/rich/git/dotnet-docker/samples/dotnetapp/bin/Release/net8.0/linux-musl-x64/publish/
  Building image 'dotnetapp' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/runtime:8.0-alpine'.
  Pushed image 'dotnetapp:latest' to local registry via 'docker'.
```

The same is true with `8.0.2xx` SDK is used with no package.

```bash
$ dotnet --version
8.0.203
$ cat dotnetapp.csproj | grep Package
$ dotnet publish --os linux-musl -t:PublishContainer -p:EnableSdkContainerSupport=true
  Building image 'dotnetapp' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/runtime:8.0-alpine'.
  Pushed image 'dotnetapp:latest' to local registry via 'docker'.
```
