# Cross-compilation

This document demonstrates how to cross-compile .NET apps as container images.

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
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
ARG TARGETARCH
WORKDIR /source

# Copy project file and restore as distinct layers
COPY --link aspnetapp/*.csproj .
RUN dotnet restore -a $TARGETARCH

# Copy source code and publish app
COPY --link aspnetapp/. .
RUN dotnet publish -a $TARGETARCH --no-restore -o /app


# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
EXPOSE 8080
WORKDIR /app
COPY --link --from=build /app .
ENTRYPOINT ["./aspnetapp"]
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
$ docker build -f Dockerfile.chiseled -t aspnetapp --platform linux/arm64 .
$ docker inspect aspnetapp | grep Arch
        "Architecture": "arm64",
$ docker run --rm aspnetapp
WARNING: The requested image's platform (linux/arm64) does not match the detected host platform (linux/amd64/v4) and no specific platform was requested
exec ./aspnetapp: exec format error
```

This image can now be pushed to a registry and pulled onto an Arm64 machine and will work.

This pattern works equally well with `docker buildx build`. It supports building multi-arch images, like `--platform linux/arm64,linux/arm32,linux/amd64`. The Dockerfile above can be built with that pattern, for multiple platforms at once.

## Publish OCI and architecture targeting

The .NET SDK has its own platform targeting model, as demonstrated by `-a $TARGETARCH` in the previous Dockerfile. A similar pattern can be used with OCI publishing.

Publish app for Arm64 (on x64 machine).

```bash
$ dotnet publish /t:PublishContainer -a arm64
$ docker images aspnetapp
REPOSITORY   TAG       IMAGE ID       CREATED          SIZE
aspnetapp    latest    2a16c87123a0   18 seconds ago   276MB
$ docker inspect aspnetapp | grep Arch
        "Architecture": "arm64",
```

## Publish multi-arch images


