# Publishing Console apps as OCI images

The .NET SDK enables generating container images and pushing them to a registry. These instructions are part of a [container workshop](README.md), which details fundamental workflows for using .NET in containers.

Related:

- [Publishing apps as OCI images](publish-oci.md)
- [Docker build publishing](dockerfile-samples.md)

## Create app

Create an app.

```bash
$ mkdir webapp
$ cd webapp
$ dotnet new web
```

## Login to the container registry

By default, the .NET SDK reads the same credentials as Docker Desktop.

The following pattern is used, for Docker Hub:

```bash
$ docker login -u richlander
```

## Publish image

The following properities (as demonstrated in the following command) can be used to build and push an image to a registry.

```bash
$ dotnet publish -p PublishProfile=DefaultContainer -p ContainerRepository=richlander/webapp -p ContainerRegistry=docker.io
```

We can then validate that presence of the image in the target registry.

```bash
$ docker pull richlander/webapp
Using default tag: latest
latest: Pulling from richlander/webapp
31ce7ceb6d44: Pull complete 
21ed37441576: Pull complete 
f87b1143177b: Pull complete 
0cb5225f39cb: Pull complete 
52da8af220c2: Pull complete 
a46f02466b30: Pull complete 
1d379052e72a: Pull complete 
Digest: sha256:9b00505738b0247d5bf17fc1d2da0a7e73cb2647a4109ab15cb7bbe647b206f5
Status: Downloaded newer image for richlander/webapp:latest
docker.io/richlander/webapp:latest
```