# Publish container images with Dockerfile

Publishing container image with a `Dockerfile` is the mainline scenario for container image publishing an what many people are most familiar.

We have a set of useful samples at [dotnet/dotnet-docker](https://github.com/dotnet/dotnet-docker). They will be used in the remainder of this document, assuming that the repo has been cloned locally.

## dotnetapp

This sample demonstrates how to build a console app.

It uses a standard multi-stage build pattern with SDK and runtime images.

```bash
$  pwd
/home/rich/git/dotnet-docker/samples/dotnetapp
$ grep FROM Dockerfile.chiseled
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
FROM mcr.microsoft.com/dotnet/runtime:10.0-noble-chiseled
```

Build the app.

```bash
$ docker build -t dotnetapp -f Dockerfile.chiseled .
$ docker images dotnetapp
REPOSITORY   TAG       IMAGE ID       CREATED         SIZE
dotnetapp    latest    c3884c20fffe   2 minutes ago   97MB
```

Run the app

```bash
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
OSDescription: Ubuntu 24.04.3 LTS
FrameworkDescription: .NET 10.0.0

UserName: app
HostName : c1b81d931f83

ProcessorCount: 24
TotalAvailableMemoryBytes: 64907018240 (60.45 GiB)
```

The app respects container limits

```bash
$ docker run --rm -m 50mb --cpus 2 dotnetapp
         42
         42              ,d                             ,d
         42              42                             42
 ,adPPYb,42  ,adPPYba, MM42MMM 8b,dPPYba,   ,adPPYba, MM42MMM
a8"    `Y42 a8"     "8a  42    42P'   `"8a a8P_____42   42
8b       42 8b       d8  42    42       42 8PP!!!!!!!   42
"8a,   ,d42 "8a,   ,a8"  42,   42       42 "8b,   ,aa   42,
 `"8bbdP"Y8  `"YbbdP"'   "Y428 42       42  `"Ybbd8"'   "Y428

OSArchitecture: X64
OSDescription: Ubuntu 24.04.3 LTS
FrameworkDescription: .NET 10.0.0

UserName: app
HostName : 47d7a2c4048a

ProcessorCount: 2
TotalAvailableMemoryBytes: 39321600 (37.50 MiB)
cgroup memory constraint: /sys/fs/cgroup/memory.max
cgroup memory limit: 52428800 (50.00 MiB)
cgroup memory usage: 6434816 (6.14 MiB)
GC Hard limit %: 75
```

## aspnetapp

This sample demonstrates how to build an ASP.NET Core app.

It uses a standard multi-stage build pattern with SDK and runtime images.

```bash
$ pwd
/home/rich/git/dotnet-docker/samples/aspnetapp
$ grep FROM Dockerfile.chiseled
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
```

Build the app.

```bash
$ docker build -t aspnetapp -f Dockerfile.chiseled .
$ docker images aspnetapp
REPOSITORY   TAG       IMAGE ID       CREATED          SIZE
aspnetapp    latest    bec338841eac   12 seconds ago   138MB
```

Run the app.

```bash
$ docker run --rm -d -p 8000:8080 -m 50mb --cpus 0.5 aspnetapp
2d7ac1ad7863da2d5708b853f5b43ca843280285883eec5a64c472fe660d726d
$  curl http://localhost:8000/Environment
{"runtimeVersion":".NET 10.0.0","osVersion":"Ubuntu 24.04.3 LTS","osArchitecture":"X64","user":"app","processorCount":1,"totalAvailableMemoryBytes":39321600,"memoryLimit":52428800,"memoryUsage":35852288,"hostName":"cea8e3897792"}
$ docker kill 2d7ac1ad7863da2d5708b853f5b43ca843280285883eec5a64c472fe660d726d
2d7ac1ad7863da2d5708b853f5b43ca843280285883eec5a64c472fe660d726d
```

Chiseled images default to using a non-root user.

## releasesapi

This app is a service and is configured to use native AOT.

It uses a standard multi-stage build pattern with SDK and runtime AOT images.

```bash
$ pwd
/home/rich/git/dotnet-docker/samples/releasesapi
$ grep FROM Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble-aot AS build
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled
```

Build the app

```bash
$ docker build --pull -t releasesapi .
$ docker images releasesapi
REPOSITORY    TAG       IMAGE ID       CREATED          SIZE
releasesapi   latest    33d53062ff66   16 minutes ago   27.4MB
```

Update the project file to [optimize for size](https://learn.microsoft.com/dotnet/core/deploying/native-aot/optimizing):

```bash
$ cat releasesapi.csproj | grep Optimization
    <OptimizationPreference>Size</OptimizationPreference>
$ cat releasesapi.csproj 
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <InvariantGlobalization>true</InvariantGlobalization>

    <!--
      Learn more about Native AOT deployment
      https://learn.microsoft.com/dotnet/core/deploying/native-aot
    -->
    <PublishAot>true</PublishAot>
    <OptimizationPreference>Size</OptimizationPreference>
  </PropertyGroup>

</Project>
```

Observe the size difference:

```bash
$ docker build --pull -t releasesapi .
$ docker images releasesapi
REPOSITORY    TAG       IMAGE ID       CREATED       SIZE
releasesapi   latest    dda01eec3bf9   3 hours ago   27.1MB
```

Small drop in size.

Run the app.

```bash
$ docker run --rm -d -p 8000:8080 releasesapi
16002fe9eedbbf7dc4d32b72334fc6ce99d37525047248507b1a07f4ef134f63
$ curl http://localhost:8000/healthz
Healthy
$ curl -s http://localhost:8000/releases | jq
{
  "reportDate": "11/11/2025",
  "versions": [
    {
      "version": "10.0",
      "supported": true,
      "eolDate": "2028-11-14",
      "supportEndsInDays": 1098,
      "releases": [
        {
          "releaseDate": "2025-11-11",
          "releasedDaysAgo": 0,
          "releaseVersion": "10.0.0",
          "security": false,
          "cveList": []
        },
        {
          "releaseDate": "2025-10-14",
          "releasedDaysAgo": 28,
          "releaseVersion": "10.0.0-rc.2",
          "security": true,
....
```

## GlobalApp

This app relies on globalization data.

Build the app.

```bash
$ pwd
/home/rich/git/dotnet-docker/samples/globalapp
$ docker build --pull -t globalapp .
$ docker images globalapp
REPOSITORY   TAG       IMAGE ID       CREATED         SIZE
globalapp    latest    47676a6edbcf   5 minutes ago   135MB
```

Run the app.

```bash
$ docker run --rm globalapp
Hello, World!

****Print baseline timezones**
Utc: (UTC) Coordinated Universal Time; 11/11/2025 21:27:24
Local: (UTC) Coordinated Universal Time; 11/11/2025 21:27:24

****Print specific timezone**
Home timezone: America/Los_Angeles
DateTime at home: 11/11/2025 13:27:24

****Culture-specific dates**
Current: 11/11/2025
English (United States) -- en-US:
11/11/2025 9:27:24 PM
11/11/2025
9:27 PM
English (Canada) -- en-CA:
2025-11-11 9:27:24 p.m.
2025-11-11
9:27 p.m.
French (Canada) -- fr-CA:
2025-11-11 21 h 27 min 24 s
2025-11-11
21 h 27
Croatian (Croatia) -- hr-HR:
11. 11. 2025. 21:27:24
11. 11. 2025.
21:27
jp (Japan) -- jp-JP:
11/11/2025 21:27:24
11/11/2025
21:27
Korean (South Korea) -- ko-KR:
2025. 11. 11. 오후 9:27:24
2025. 11. 11.
오후 9:27
Portuguese (Brazil) -- pt-BR:
11/11/2025 21:27:24
11/11/2025
21:27
Chinese (China) -- zh-CN:
2025/11/11 21:27:24
2025/11/11
21:27

****Culture-specific currency:**
Current: ¤1,337.00
en-US: $1,337.00
en-CA: $1,337.00
fr-CA: 1 337,00 $
hr-HR: 1.337,00 €
jp-JP: ¥ 1337
ko-KR: ₩1,337
pt-BR: R$ 1.337,00
zh-CN: ¥1,337.00

****Japanese calendar**
08/18/2019
01/08/18
平成元年8月18日
平成元年8月18日

****String comparison**
Comparison results: `0` mean equal, `-1` is less than and `1` is greater
Test: compare i to (Turkish) İ; first test should be equal and second not
0
-1
Test: compare Å Å; should be equal
0
```

We can build also build images for Alpine and Ubuntu Chiseled, with significantly smaller sizes.

```bash
$ grep FROM Dockerfile.alpine 
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine-extra
$ docker build --pull -t globalapp -f Dockerfile.alpine .
$ docker images globalapp
REPOSITORY   TAG       IMAGE ID       CREATED         SIZE
globalapp    latest    064a4e159d3d   3 seconds ago   63.4MB
$ grep FROM Dockerfile.ubuntu-chiseled
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled-extra
$ docker build --pull -t globalapp -f Dockerfile.ubuntu-chiseled .
$ docker images globalapp
REPOSITORY   TAG       IMAGE ID       CREATED         SIZE
globalapp    latest    804b6fcbc689   4 seconds ago   67.2MB
```
