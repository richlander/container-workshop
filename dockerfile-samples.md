# Publish container images with Dockerfile

Publishing container image with a `Dockerfile` is the mainline scenario for container image publishing an what many people are most familiar.

We have a set of useful samples at [dotnet/dotnet-docker](https://github.com/dotnet/dotnet-docker). They will be used in the remainder of this document, assuming that the repo has been cloned locally.

## dotnetapp

This sample demonstrates how to build a console app.

It uses a standard multi-stage build pattern with SDK and runtime images.

```bash
$ grep FROM Dockerfile.chiseled
FROM --platform=$BUILDPLATFORM  mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled
```

Build the app.

```bash
$  pwd
/home/rich/git/dotnet-docker/samples/dotnetapp
$ docker build -t dotnetapp -f Dockerfile.chiseled .
$ docker images dotnetapp
REPOSITORY   TAG       IMAGE ID       CREATED          SIZE
dotnetapp    latest    e17f4e355cea   46 seconds ago   85.4MB
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
OSDescription: Ubuntu 22.04.3 LTS
FrameworkDescription: .NET 8.0.0-rc.2.23479.6

UserName: app
HostName : 2b20849391c2

ProcessorCount: 8
TotalAvailableMemoryBytes: 33258950656 (30.97 GiB)
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
OSDescription: Ubuntu 22.04.3 LTS
FrameworkDescription: .NET 8.0.0-rc.2.23479.6

UserName: app
HostName : 5fb1d6bd3c2e

ProcessorCount: 2
TotalAvailableMemoryBytes: 39321600 (37.50 MiB)
cgroup memory constraint: /sys/fs/cgroup/memory.max
cgroup memory limit: 52428800 (50.00 MiB)
cgroup memory usage: 6320128 (6.03 MiB)
GC Hard limit %: 75
```

## aspnetapp

This sample demonstrates how to build an ASP.NET Core app.

It uses a standard multi-stage build pattern with SDK and runtime images.

```bash
$ grep FROM Dockerfile.chiseled
FROM --platform=$BUILDPLATFORM  mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled
```

Build the app.

```bash
$ docker build -t aspnetapp -f Dockerfile.chiseled .
$ docker images aspnetapp
REPOSITORY   TAG       IMAGE ID       CREATED          SIZE
aspnetapp    latest    7095176b6456   59 seconds ago   118MB
```

Run the app.

```bash
$ docker run --rm -d -p 8000:8080 -m 50mb --cpus 0.5 aspnetapp
2d7ac1ad7863da2d5708b853f5b43ca843280285883eec5a64c472fe660d726d
$  curl http://localhost:8000/Environment
{"runtimeVersion":".NET 8.0.0-rc.2.23479.6","osVersion":"Ubuntu 22.04.3 LTS","osArchitecture":"X64","user":"app","processorCount":1,"totalAvailableMemoryBytes":39321600,"memoryLimit":52428800,"memoryUsage":29884416,"hostName":"2d7ac1ad7863"}
$ docker kill 2d7ac1ad7863da2d5708b853f5b43ca843280285883eec5a64c472fe660d726d
2d7ac1ad7863da2d5708b853f5b43ca843280285883eec5a64c472fe660d726d
```

Chiseled images default to using a non-root user.

## releasesapi

This app is a service and is configured to use native AOT.

It uses a standard multi-stage build pattern with SDK and runtime AOT images.

```bash
$ grep FROM Dockerfile.ubuntu-chiseled
FROM mcr.microsoft.com/dotnet/nightly/sdk:8.0-jammy-aot AS build
FROM mcr.microsoft.com/dotnet/nightly/runtime-deps:8.0-jammy-chiseled-aot
```

Build the app

```bash
$ docker build --pull -t releasesapi -f Dockerfile.ubuntu-chiseled .
$ docker images releasesapi
REPOSITORY    TAG       IMAGE ID       CREATED              SIZE
releasesapi   latest    fbf760a2ae3b   About a minute ago   25.4MB
```

Run the app.

```bash
$ docker run --rm -d -p 8000:8080 releasesapi
16002fe9eedbbf7dc4d32b72334fc6ce99d37525047248507b1a07f4ef134f63
$ curl http://localhost:8000/healthz
Healthy
$ curl -s http://localhost:8000/releases | jq
{
  "report-date": "11/04/2023",
  "versions": [
    {
      "version": "8.0",
      "supported": false,
      "eol-date": "",
      "support-ends-in-days": 0,
      "releases": [
        {
          "release-date": "2023-10-10",
          "released-days-ago": 25,
          "release-version": "8.0.0-rc.2",
          "security": true,
....
```

## GlobalApp

This app relies on globalization data.

Build the app.

```bash
$ docker build --pull -t globalapp .
$ docker images globalapp
REPOSITORY   TAG       IMAGE ID       CREATED         SIZE
globalapp    latest    82a6e2638126   5 minutes ago   193MB
```

Run the app.

```bash
$ docker run --rm globalapp
Hello, World!

****Print baseline timezones**
Utc: (UTC) Coordinated Universal Time; 11/04/2023 06:48:22
Local: (UTC) Coordinated Universal Time; 11/04/2023 06:48:22

****Print specific timezone**
Home timezone: America/Los_Angeles
DateTime at home: 11/03/2023 23:48:22

****Culture-specific dates**
Current: 11/04/2023
English (United States) -- en-US:
11/4/2023 6:48:22 AM
11/4/2023
6:48 AM
English (Canada) -- en-CA:
11/4/2023 6:48:22 a.m.
11/4/2023
6:48 a.m.
French (Canada) -- fr-CA:
2023-11-04 06 h 48 min 22 s
2023-11-04
06 h 48
Croatian (Croatia) -- hr-HR:
04. 11. 2023. 06:48:22
04. 11. 2023.
06:48
jp (Japan) -- jp-JP:
11/4/2023 06:48:22
11/4/2023
06:48
Korean (South Korea) -- ko-KR:
2023. 11. 4. 오전 6:48:22
2023. 11. 4.
오전 6:48
Portuguese (Brazil) -- pt-BR:
04/11/2023 06:48:22
04/11/2023
06:48
Chinese (China) -- zh-CN:
2023/11/4 06:48:22
2023/11/4
06:48

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

We can switch the app to use a chiseled image that includes globalization libraries.

```Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
WORKDIR /source

COPY . .
RUN dotnet publish --sc -p PublishTrimmed=true -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-jammy-chiseled-extra
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["./globalapp"]
```

Re-build the app.

```bash
$ docker build --pull -t globalapp .
$ docker images globalapp
REPOSITORY   TAG       IMAGE ID       CREATED         SIZE
globalapp    latest    c592b02403d6   9 seconds ago   71.1MB
```

Re-run the app.

```bash
docker run --rm globalapp
Hello, World!

****Print baseline timezones**
Utc: (UTC) Coordinated Universal Time; 11/04/2023 06:54:05
Local: (UTC) Coordinated Universal Time; 11/04/2023 06:54:05

****Print specific timezone**
Home timezone: America/Los_Angeles
DateTime at home: 11/03/2023 23:54:05

....
```

The output is the same.
