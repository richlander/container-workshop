# OCI image publishing reference

This document lists the properties needing to [publish OCI images](publish-oci.md). It is part of a [container workshop](README.md), which details fundamental workflows for using .NET in containers.

## Enable publishing

OCI image publishing is not enabled by default with `dotnet publish`. Most users do not want to generate a container image every time they publish their project, but an app they can run directly.

Container publishing can be enabled with one of the following gestures:

- `PublishContainer` -- Include this target with `-t:PublishContainer` on the commandline.
- `PublishProfile` -- Set this property with `-p PublishProfile=DefaultContainer` on the commandline or in a project file. This property is only available to ASP.NET Core apps.

Console apps additionally require installing a NuGet package.

```bash
$ dotnet add package Microsoft.NET.Build.Containers --version 8.0.100-rc.2.23480.5
```

## Configure publishing

These properties can be used to configure OCI image publishing.

- `ContainerFamily` -- Use a specific family of base images, such as with `-p ContainerFamily=jammy-chiseled` or `-p ContainerFamily=alpine`.
- `ContainerRepository` -- Use a different image name than the default, such as with `-p ContainerRepository=mycustomerimagename`.
- `ContainerRegistry` -- The registry address to push to, such as `docker.io` or `myregistry.azurecr.io`.
- `ContainerArchiveOutputPath` -- Publishes the image as a tarball to the specified directory.

These container properties can be specified in a project file in a `ProperyGroup` section, as follows.

```xml
<ContainerFamily>jammy-chiseled</ContainerFamily>
<ContainerRepository>mycustomerimagename</ContainerRepository>
<ContainerRegistry>docker.io</ContainerRegistry>
```
