# Publishing Ubuntu chiseled images

The [hello-dotnet app](./publish-oci.md) can be published with an additional property -- `ContainerFamily` -- to target Ubuntu chiseled images. Chiseled images include fewer components and are much smaller as a result.

```bash
$ dotnet publish /t:PublishContainer /p:ContainerFamily=jammy-chiseled
$ docker run --rm hello-dotnet
Hello, Ubuntu 22.04.3 LTS!
```

Based image: `mcr.microsoft.com/dotnet/runtime:8.0-jammy-chiseled`

Note: `jammy-chiseled` does not include the ICU or `tzdata` packages. Use `jammy-chiseled-extra` if you need those packages.

Inspect image:

```bash
$ docker images hello-dotnet
REPOSITORY     TAG       IMAGE ID       CREATED              SIZE
hello-dotnet   latest    114893f0476f   About a minute ago   85.4MB
```

The images can be made smaller using [.NET SDK publishing options](./publish-options.md).

## No shell

Chiseled images don't contain a shell. Let's try to use bash.

```bash
$ docker run --rm --entrypoint bash hello-dotnet
docker: Error response from daemon: failed to create task for container: failed to create shim task: OCI runtime create failed: runc create failed: unable to start container process: exec: "bash": executable file not found in $PATH: unknown.
```

Running the entrypoint fails because `bash` isn't in the chiseled images.
