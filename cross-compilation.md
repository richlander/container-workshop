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

It is common to need both Arm64 and x64 images, to support heterogeneous developer, CI, and production deployments. `dotnet publish` can produce multi-arch (AKA manifest-based) images. It can be part of multi-arch workflows supported by local container cache, local registry, or remote registry.

`dotnet publish` relies on the `RuntimeIdentifiers` (plural) property being set to inform which architectures to generate.

```bash
$ pwd
/home/rich/git/dotnet-docker/samples/aspnetapp/aspnetapp
$ grep Runtime aspnetapp.csproj
    <RuntimeIdentifiers>linux-x64;linux-arm64</RuntimeIdentifiers>
$ dotnet publish -t:PublishContainer
Restore complete (0.2s)
  aspnetapp net10.0 linux-x64 succeeded (0.7s) → bin/Release/net10.0/linux-x64/publish/
  aspnetapp net10.0 linux-arm64 succeeded (1.0s) → bin/Release/net10.0/linux-arm64/publish/
  aspnetapp net10.0 succeeded (0.3s) → bin/Release/net10.0/publish/

Build succeeded in 2.5s
$ docker images aspnetapp
REPOSITORY   TAG       IMAGE ID       CREATED         SIZE
aspnetapp    latest    7a181e990202   9 seconds ago   458MB
$ docker ps
CONTAINER ID   IMAGE        COMMAND                  CREATED       STATUS       PORTS                                       NAMES
d4b199dae300   registry:3   "/entrypoint.sh /etc…"   4 hours ago   Up 4 hours   0.0.0.0:5000->5000/tcp, :::5000->5000/tcp   registry
rich@merritt:~/git/dotnet-docker/samples/aspnetapp/aspnetapp$ docker
$ docker tag aspnetapp localhost:5000/aspnetapp
$ docker push localhost:5000/aspnetapp
$ docker manifest inspect localhost:5000/aspnetapp
no such manifest: localhost:5000/aspnetapp:latest
```

`docker manifest inspect` doesn't work with insecure registries. We can use `curl` instead to see what we pushed to the registry.

```bash
$ curl -s http://localhost:5000/v2/aspnetapp/tags/list | jq
{
  "name": "aspnetapp",
  "tags": [
    "latest"
  ]
}
$ curl -s   -H 'Accept: application/vnd.oci.image.index.v1+json,application/vnd.docker.distribution.manifest.list.v2+json,application/vnd.docker.distribution.manifest.v2+json'   http://localhost:5000/v2/aspnetapp/manifests/latest | jq
{
  "schemaVersion": 2,
  "mediaType": "application/vnd.oci.image.index.v1+json",
  "manifests": [
    {
      "mediaType": "application/vnd.oci.image.manifest.v1+json",
      "size": 1402,
      "digest": "sha256:5432898e9448e9a00418d7c59ab0ba4dab3f4c426d931e402b81535f80e8a7d2",
      "platform": {
        "architecture": "amd64",
        "os": "linux"
      }
    },
    {
      "mediaType": "application/vnd.oci.image.manifest.v1+json",
      "size": 1402,
      "digest": "sha256:0232c72cda710ea61c63b2c25c973b253d731fae545a7a0e912ef01a6d8fecce",
      "platform": {
        "architecture": "arm64",
        "os": "linux"
      }
    }
  ]
}
```

The Skopeo tool has better support for inspecting insecure registries.

```bash
$ skopeo inspect --raw --tls-verify=false docker://localhost:5000/aspnetapp:latest | jq
{
  "schemaVersion": 2,
  "mediaType": "application/vnd.oci.image.index.v1+json",
  "manifests": [
    {
      "mediaType": "application/vnd.oci.image.manifest.v1+json",
      "size": 1402,
      "digest": "sha256:5432898e9448e9a00418d7c59ab0ba4dab3f4c426d931e402b81535f80e8a7d2",
      "platform": {
        "architecture": "amd64",
        "os": "linux"
      }
    },
    {
      "mediaType": "application/vnd.oci.image.manifest.v1+json",
      "size": 1402,
      "digest": "sha256:0232c72cda710ea61c63b2c25c973b253d731fae545a7a0e912ef01a6d8fecce",
      "platform": {
        "architecture": "arm64",
        "os": "linux"
      }
    }
  ]
}
```

The tool returns a manifest list with two Linux entries, for x64 and Arm64.

We can also look at the image manifests.


```bash
$ skopeo inspect --tls-verify=false docker://localhost:5000/aspnetapp:latest | jq
{
  "Name": "localhost:5000/aspnetapp",
  "Digest": "sha256:7a181e99020207cd45d3e556c5cd4f539756518c7518868629e6b5f878e4a169",
  "RepoTags": [
    "latest"
  ],
  "Created": "2025-11-12T17:50:30.6756493Z",
  "DockerVersion": "",
  "Labels": {
    "net.dot.runtime.majorminor": "10.0",
    "net.dot.sdk.version": "10.0.100",
    "org.opencontainers.artifact.created": "2025-11-12T17",
    "org.opencontainers.image.authors": "aspnetapp",
    "org.opencontainers.image.base.digest": "sha256:95a6f1e5c7cbc16575ea6d328f4f5614f801633752bb8f0185b71489523e94eb",
    "org.opencontainers.image.base.name": "mcr.microsoft.com/dotnet/aspnet",
    "org.opencontainers.image.created": "2025-11-12T17",
    "org.opencontainers.image.ref.name": "ubuntu",
    "org.opencontainers.image.version": "1.0.0"
  },
  "Architecture": "amd64",
  "Os": "linux",
  "Layers": [
    "sha256:20043066d3d5c78b45520c5707319835ac7d1f3d7f0dded0138ea0897d6a3188",
    "sha256:05a997a818e912916b5455ed2a572f4f1779812770cf6301e11fb2f86b92a136",
    "sha256:e556599eb01843481eb2ced4641c7afea2a15f40af57aa236dae733bac7035f4",
    "sha256:66ecb3299fecbc0d05e411d7a62b0e25ba005c3ed4ad1ebdcd89c2692ab95415",
    "sha256:cf846b836998163e04850ddb5c7e2210eb8c95bc0c221230c5d6a26cc7de63e7",
    "sha256:ad3a33b726b12ed0a2013a69491146deda74c174c57edbfb0d07a7a536d1d1e5",
    "sha256:442d8a1b7ca836c49c18c6166d84d42558984f2f9ac7b2fe524c3549ae80544f"
  ],
  "LayersData": [
    {
      "MIMEType": "application/vnd.docker.image.rootfs.diff.tar.gzip",
      "Digest": "sha256:20043066d3d5c78b45520c5707319835ac7d1f3d7f0dded0138ea0897d6a3188",
      "Size": 29724688,
      "Annotations": null
    },
    {
      "MIMEType": "application/vnd.docker.image.rootfs.diff.tar.gzip",
      "Digest": "sha256:05a997a818e912916b5455ed2a572f4f1779812770cf6301e11fb2f86b92a136",
      "Size": 16817633,
      "Annotations": null
    },
    {
      "MIMEType": "application/vnd.docker.image.rootfs.diff.tar.gzip",
      "Digest": "sha256:e556599eb01843481eb2ced4641c7afea2a15f40af57aa236dae733bac7035f4",
      "Size": 3535,
      "Annotations": null
    },
    {
      "MIMEType": "application/vnd.docker.image.rootfs.diff.tar.gzip",
      "Digest": "sha256:66ecb3299fecbc0d05e411d7a62b0e25ba005c3ed4ad1ebdcd89c2692ab95415",
      "Size": 36707938,
      "Annotations": null
    },
    {
      "MIMEType": "application/vnd.docker.image.rootfs.diff.tar.gzip",
      "Digest": "sha256:cf846b836998163e04850ddb5c7e2210eb8c95bc0c221230c5d6a26cc7de63e7",
      "Size": 155,
      "Annotations": null
    },
    {
      "MIMEType": "application/vnd.docker.image.rootfs.diff.tar.gzip",
      "Digest": "sha256:ad3a33b726b12ed0a2013a69491146deda74c174c57edbfb0d07a7a536d1d1e5",
      "Size": 12759656,
      "Annotations": null
    },
    {
      "MIMEType": "application/vnd.oci.image.layer.v1.tar+gzip",
      "Digest": "sha256:442d8a1b7ca836c49c18c6166d84d42558984f2f9ac7b2fe524c3549ae80544f",
      "Size": 5426998,
      "Annotations": null
    }
  ],
  "Env": [
    "PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin",
    "APP_UID=1654",
    "ASPNETCORE_HTTP_PORTS=8080",
    "DOTNET_RUNNING_IN_CONTAINER=true",
    "DOTNET_VERSION=10.0.0",
    "ASPNET_VERSION=10.0.0"
  ]
}
$ skopeo inspect --tls-verify=false docker://localhost:5000/aspnetapp:latest --override-arch arm64 | jq
{
  "Name": "localhost:5000/aspnetapp",
  "Digest": "sha256:7a181e99020207cd45d3e556c5cd4f539756518c7518868629e6b5f878e4a169",
  "RepoTags": [
    "latest"
  ],
  "Created": "2025-11-12T17:50:31.1747602Z",
  "DockerVersion": "",
  "Labels": {
    "net.dot.runtime.majorminor": "10.0",
    "net.dot.sdk.version": "10.0.100",
    "org.opencontainers.artifact.created": "2025-11-12T17",
    "org.opencontainers.image.authors": "aspnetapp",
    "org.opencontainers.image.base.digest": "sha256:07ee19ab1489efda0f64b9701a7ea6a02522001f2fee901031046d07ec217b23",
    "org.opencontainers.image.base.name": "mcr.microsoft.com/dotnet/aspnet",
    "org.opencontainers.image.created": "2025-11-12T17",
    "org.opencontainers.image.ref.name": "ubuntu",
    "org.opencontainers.image.version": "1.0.0"
  },
  "Architecture": "arm64",
  "Os": "linux",
  "Layers": [
    "sha256:97dd3f0ce510a30a2868ff104e9ff286ffc0ef01284aebe383ea81e85e26a415",
    "sha256:5c57a8c859e2e325b62a2c81fc3da56db2274f2084df97f9fa118f751c1f0d76",
    "sha256:7d9e1832d26ac2daa9e6af016a0da63d925a22c72c7673707338d057ccc3a068",
    "sha256:7cce16645c484da5db798ea1c93ef06576e4c7a0cd534a13079d30a883e35cb1",
    "sha256:d95936f442c561a911722daba76c39964bc169290e61b297c0ddac14ec04baa5",
    "sha256:724261c1460e0b2b2cd34e459af32d077d3a217b3314377de3818e4b0c1ce79a",
    "sha256:0964bf3bcc21259a9c1e9377c280de7dd2939caf13f6d32d58090ae211538e39"
  ],
  "LayersData": [
    {
      "MIMEType": "application/vnd.docker.image.rootfs.diff.tar.gzip",
      "Digest": "sha256:97dd3f0ce510a30a2868ff104e9ff286ffc0ef01284aebe383ea81e85e26a415",
      "Size": 28861957,
      "Annotations": null
    },
    {
      "MIMEType": "application/vnd.docker.image.rootfs.diff.tar.gzip",
      "Digest": "sha256:5c57a8c859e2e325b62a2c81fc3da56db2274f2084df97f9fa118f751c1f0d76",
      "Size": 16792838,
      "Annotations": null
    },
    {
      "MIMEType": "application/vnd.docker.image.rootfs.diff.tar.gzip",
      "Digest": "sha256:7d9e1832d26ac2daa9e6af016a0da63d925a22c72c7673707338d057ccc3a068",
      "Size": 3561,
      "Annotations": null
    },
    {
      "MIMEType": "application/vnd.docker.image.rootfs.diff.tar.gzip",
      "Digest": "sha256:7cce16645c484da5db798ea1c93ef06576e4c7a0cd534a13079d30a883e35cb1",
      "Size": 34614464,
      "Annotations": null
    },
    {
      "MIMEType": "application/vnd.docker.image.rootfs.diff.tar.gzip",
      "Digest": "sha256:d95936f442c561a911722daba76c39964bc169290e61b297c0ddac14ec04baa5",
      "Size": 154,
      "Annotations": null
    },
    {
      "MIMEType": "application/vnd.docker.image.rootfs.diff.tar.gzip",
      "Digest": "sha256:724261c1460e0b2b2cd34e459af32d077d3a217b3314377de3818e4b0c1ce79a",
      "Size": 12250652,
      "Annotations": null
    },
    {
      "MIMEType": "application/vnd.oci.image.layer.v1.tar+gzip",
      "Digest": "sha256:0964bf3bcc21259a9c1e9377c280de7dd2939caf13f6d32d58090ae211538e39",
      "Size": 5426396,
      "Annotations": null
    }
  ],
  "Env": [
    "PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin",
    "APP_UID=1654",
    "ASPNETCORE_HTTP_PORTS=8080",
    "DOTNET_RUNNING_IN_CONTAINER=true",
    "DOTNET_VERSION=10.0.0",
    "ASPNET_VERSION=10.0.0"
  ]
}
```
