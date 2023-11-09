# Publish OCI image in SDK container

This document demonstrates how to publish .NET web apps as container images, relying on .NET SDK container images. It is part of a [container workshop](README.md), which details fundamental workflows for using .NET in containers. 

The following patterns rely on [OCI image publishing](https://learn.microsoft.com/dotnet/core/docker/publish-as-container). They run `dotnet publish` within an SDK container, avoiding the need to install .NET (and other dependencies) locally. This is particularly useful for native AOT, which is used in the examples. The overall pattern isn't specific to native AOT.

Native AOT SDK container images are used in the instructions: `mcr.microsoft.com/dotnet/nightly/sdk:8.0-jammy-aot`.

For non-native AOT use cases, the smaller SDK image can be used, such as `mcr.microsoft.com/dotnet/sdk:8.0-jammy`.

## Acquire app

In the most typical case, an app would be aquired by git clone. In this example, a new AOT app will be created. If you'd rather acquire an app (and don't have one), try this one: https://github.com/dotnet/dotnet-docker/tree/main/samples/releasesapi.

```bash
$ mkdir hello-native-api
$ cd hello-native-api/
$ dotnet new webapiaot
```

## Run app locally

The app can be run locally.

```bash
$ dotnet run
Building...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5099
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
info: Microsoft.Hosting.Lifetime[0]
      Content root path: /home/rich/hello-native-api
```

The port on your machine will likely differ.

Call the service in another window:

```bash
$ curl http://localhost:5099/todos
[{"id":1,"title":"Walk the dog","dueBy":null,"isComplete":false},{"id":2,"title":"Do the dishes","dueBy":"2023-11-03","isComplete":false},{"id":3,"title":"Do the laundry","dueBy":"2023-11-04","isComplete":false},{"id":4,"title":"Clean the bathroom","dueBy":null,"isComplete":false},{"id":5,"title":"Clean the car","dueBy":"2023-11-05","isComplete":false}]
```

## Add new end-point

An end-point that returns `RuntimeInformation.OSDescription` would be nice. It can be added to `Program.cs` 

```csharp
app.MapGet("/os", () => $$"""{"os-description" : "{{System.Runtime.InteropServices.RuntimeInformation.OSDescription}}"}{{Environment.NewLine}}""");

app.Run();
```

That's not the most idiomatic C#. Returning an object and relying on automatic serialization would be more typical. However, that code is the best we can get in one line. It also demonstrates [interpolated raw string literals](https://learn.microsoft.com/dotnet/csharp/language-reference/tokens/interpolated#interpolated-raw-string-literals).

Once the app is re-run, the new end-point can be called and pretty-printed with `jq`.

```bash
$ curl -s http://localhost:5099/os | jq
{
  "os-description": "Ubuntu 22.04.3 LTS"
}
```

## Update the project file

The project file should be updated to include the optimal settings and to avoid clutter of the command line. We won't need that right way.

Add to the `PropertyGroup` section:

```xml
<ContainerBaseImage>mcr.microsoft.com/dotnet/nightly/runtime-deps:8.0-jammy-chiseled-aot</ContainerBaseImage>
```

## Build bare binary locally

Let's now assume that .NET 8 and `clang` are not installed locally. We can use a native AOT SDK container image. This pattern doesn't produce a container image, but is (A) uniquely useful, and (B) is a step on the way (in terms of building blocks) to producing a container image.

The SDK container is experimental and requires a [`nuget.config`](https://gist.github.com/richlander/4a700d1679e42b7868805c0780ab173c) to work correctly.

```bash
$ curl -LO https://gist.githubusercontent.com/richlander/4a700d1679e42b7868805c0780ab173c/raw/cf3e9dccfeaa2ef33c7376d7c95c99284e83fbb3/nuget.config
```

Build the app, in the SDK container

```bash
$ docker run --rm -it -v $(pwd):/source -w /source mcr.microsoft.com/dotnet/nightly/sdk:8.0-jammy-aot dotnet publish -o app
$ ls -l app
total 31216
drwxr-xr-x 2 root root     4096 Nov  3 21:03 app
-rw-rw-r-- 1 root root      127 Nov  3 19:38 appsettings.Development.json
-rw-rw-r-- 1 root root      151 Nov  3 19:38 appsettings.json
-rwxr-xr-x 1 root root 10473280 Nov  3 21:03 hello-native-api
-rwxr-xr-x 1 root root 21472640 Nov  3 21:03 hello-native-api.dbg
-rw-rw-r-- 1 root root      299 Nov  3 20:01 nuget.config
```

The app is now available locally. It's about 10MB.

It can be run in a similar way. It will run in a Linux environment, since the container images builds a Linux binary.

```bash
$ ./app/hello-native-api
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Production
info: Microsoft.Hosting.Lifetime[0]
      Content root path: /home/rich/hello-native-api
```

This time, it is hosted on port `5000`.

```bash
$ curl -s http://localhost:5000/os
{"os-description" : "Ubuntu 22.04.3 LTS"}
```

## Build a container image

This pattern can be taken one step further, to build a container image. We're going to use a similar volume mounting technique, but with a [tarball archive as the output](https://github.com/dotnet/core/issues/8440#issuecomment-1743593480) using the `ContainerArchiveOutputPath` property.

```bash
$ docker run --rm -it -v $(pwd):/source -w /source mcr.microsoft.com/dotnet/nightly/sdk:8.0-jammy-aot dotnet publish -p PublishProfile=DefaultContainer -p ContainerArchiveOutputPath=image/hello-native-api.tar.gz
$ ls image/
hello-native-api.tar.gz
$ docker load --input image/hello-native-api.tar.gz 
b97559ee6916: Loading layer  10.66MB/10.66MB
Loaded image: hello-native-api:latest
$ docker images hello-native-api
REPOSITORY         TAG       IMAGE ID       CREATED         SIZE
hello-native-api   latest    caf8cdaf5e79   2 minutes ago   42.7MB
```

That worked. The image was published inside a container image, written to the local machine via a volume mount, and then loaded into the local docker cache via `docker load`.

The difference in size is that the 10MB value is compressed and the 42MB value is uncompressed.

The publish command should look like:

```bash
$ docker run --rm -it -v $(pwd):/source -w /source mcr.microsoft.com/dotnet/nightly/sdk:8.0-jammy-aot dotnet publish -p PublishProfile=DefaultContainer -p ContainerArchiveOutputPath=image/hello-native-api.tar.gz
MSBuild version 17.8.3+195e7f5a3 for .NET
  Determining projects to restore...
  Restored /source/hello-native-api.csproj (in 8.1 sec).
/usr/share/dotnet/sdk/8.0.100-rtm.23523.2/Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.RuntimeIdentifierInference.targets(311,5): message NETSDK1057: You are using a preview version of .NET. See: https://aka.ms/dotnet-support-policy [/source/hello-native-api.csproj]
  hello-native-api -> /source/bin/Release/net8.0/linux-x64/hello-native-api.dll
  hello-native-api -> /source/bin/Release/net8.0/linux-x64/publish/
  Building image 'hello-native-api' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/nightly/runtime-deps:8.0-jammy-chiseled-aot'.
  Pushed image 'hello-native-api:latest' to local archive at '/source/image/hello-native-api.tar.gz'.
```

The last line calls out that the image has been written to a local archive path.

## Publish container image to a remote registry

The image can be published to a remote registry, using much the same pattern. There are some key differences.

- `ContainerRepository` must specify the image name plus any "org" information.
- `ContainerRegistry` must specify the registry name, like `docker.io` or `foo.azurecr.io`.
- [Credentials must be provided](https://github.com/dotnet/sdk-container-builds/blob/main/docs/RegistryAuthentication.md) to push to a registry.

Credentials can be provided in two ways.

- Pass credentials as [environment variables](https://github.com/dotnet/sdk-container-builds/issues/486).
- Volume mount `.docker/config.json`. This approach only works in environments where credentials are left unencrypted (primarily, Linux) or via [custom-generated json file](https://github.com/dotnet/sdk-container-builds/issues/484#issuecomment-1657048065) using the same format.

For this scenario, I'm going to volume mount `.docker/config.json` and login to https://hub.docker.com/.

```bash
$ docker login -u richlander
Password: 
WARNING! Your password will be stored unencrypted in /home/rich/.docker/config.json.
```

And then publish

```bash
$ docker run --rm -it -v $(pwd):/source -w /source -v /home/rich/.docker:/root/.docker mcr.microsoft.com/dotnet/nightly/sdk:8.0-jammy-aot dotnet publish -p PublishProfile=DefaultContainer -p ContainerRepository=richlander/hello-native-api -p ContainerRegistry=docker.io
MSBuild version 17.8.3+195e7f5a3 for .NET
  Determining projects to restore...
  Restored /source/hello-native-api.csproj (in 8.47 sec).
/usr/share/dotnet/sdk/8.0.100-rtm.23523.2/Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.RuntimeIdentifierInference.targets(311,5): message NETSDK1057: You are using a preview version of .NET. See: https://aka.ms/dotnet-support-policy [/source/hello-native-api.csproj]
  hello-native-api -> /source/bin/Release/net8.0/linux-x64/hello-native-api.dll
  hello-native-api -> /source/bin/Release/net8.0/linux-x64/publish/
  Building image 'richlander/hello-native-api' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/nightly/runtime-deps:8.0-jammy-chiseled-aot'.
  Uploading layer 'sha256:2cf7030f21c01c0712d16119d6d7109c7cef1e5d5c24a006f771bbfdb414a865' to 'docker.io'.
  Uploading config to registry at blob 'sha256:ac72a5f9b5ac25e091f68a400ec17c540f35a323512b4549ea229d00c3d9d415',
  Uploaded config to registry.
  Uploading tag 'latest' to 'docker.io'.
  Uploaded tag 'latest' to 'docker.io'.
  Pushed image 'richlander/hello-native-api:latest' to registry 'docker.io'.
```

I can now run the app.

```bash
 docker run --rm -it -p 8000:8080 richlander/hello-native-api
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://[::]:8080
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Production
info: Microsoft.Hosting.Lifetime[0]
      Content root path: /app
^Cinfo: Microsoft.Hosting.Lifetime[0]
```

And from another terminal.

```bash
$ curl http://localhost:8000/os
{"os-description" : "Ubuntu 22.04.3 LTS"}
```

I can also access the endpoint from another machine on the same network.

```bash
$ curl http://vancouver:8000/os
{"os-description" : "Ubuntu 22.04.3 LTS"}
```

## Publish container image to a local registry

It is possible to host a local registry using the [`registry`](https://hub.docker.com/_/registry) image. This doesn't currently work, unless a TLS certificate is used.

Launch a local registry instance

```bash
$ docker run -d -p 5000:5000 registry
```

Publish the image and push to the local registry.

```bash
$ $ docker run --add-host=host.docker.internal:host-gateway --rm -it -v $(pwd):/source -w /source mcr.microsoft.com/dotnet/nightly/sdk:8.0-jammy-aot dotnet publish -p PublishProfile=DefaultContainer -p ContainerRepository=hello-native-api -p ContainerRegistry=http://localhost:5000
MSBuild version 17.8.3+195e7f5a3 for .NET
  Determining projects to restore...
  Restored /source/hello-native-api.csproj (in 4.29 sec).
/usr/share/dotnet/sdk/8.0.100-rtm.23523.2/Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.RuntimeIdentifierInference.targets(311,5): message NETSDK1057: You are using a preview version of .NET. See: https://aka.ms/dotnet-support-policy [/source/hello-native-api.csproj]
  hello-native-api -> /source/bin/Release/net8.0/linux-x64/hello-native-api.dll
  hello-native-api -> /source/bin/Release/net8.0/linux-x64/publish/
/usr/share/dotnet/sdk/8.0.100-rtm.23523.2/Containers/build/Microsoft.NET.Build.Containers.targets(117,5): error CONTAINER2012: Could not recognize registry 'http://localhost:5000'. [/source/hello-native-api.csproj]
```

This currently fails due to a lack of TLS. Looks like it is due to [dotnet/sdk-container-builds #338](https://github.com/dotnet/sdk-container-builds/issues/338).
