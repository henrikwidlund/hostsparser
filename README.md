# HostsParser

[![Publish Filter](https://img.shields.io/github/workflow/status/henrikwidlund/hostsparser/Publish%20Filter?label=Publish%20Filter&logo=github)](https://github.com/henrikwidlund/hostsparser/actions/workflows/publish-filter.yml)
[![CI](https://img.shields.io/github/workflow/status/henrikwidlund/hostsparser/Build%20and%20Test?label=CI&logo=github)](https://github.com/henrikwidlund/hostsparser/actions/workflows/ci.yml)
[![CodeQL](https://img.shields.io/github/workflow/status/henrikwidlund/hostsparser/CodeQL?label=CodeQL&logo=github)](https://github.com/henrikwidlund/hostsparser/actions/workflows/codeql-analysis.yml)
[![Docker](https://img.shields.io/github/workflow/status/henrikwidlund/hostsparser/Docker?label=Docker&logo=docker)](https://github.com/henrikwidlund/hostsparser/actions/workflows/docker.yml)
[![codecov.io](https://img.shields.io/codecov/c/gh/henrikwidlund/hostsparser?label=codecov&logo=codecov)](https://codecov.io/gh/henrikwidlund/hostsparser)

Tool that converts a `hosts` ([`HostsBased`](#hostsbased)) based file into a `AdBlock` formatted file, optimized for [AdGuard Home](https://github.com/AdguardTeam/AdGuardHome).
It also removes duplicates, comments as well as hosts that are already blocked by a different `AdBlock` ([`AdBlockBased`](#adblockbased)) based file.

By default [StevenBlack/hosts](https://github.com/StevenBlack/hosts) 
[with fakenews, gambling and porn extensions](https://raw.githubusercontent.com/StevenBlack/hosts/master/alternates/fakenews-gambling-porn/hosts)
is processed to exclude entries already covered by the [AdGuard DNS Filter](https://github.com/AdguardTeam/AdGuardSDNSFilter)
[file](https://adguardteam.github.io/AdGuardSDNSFilter/Filters/filter.txt).

**Note** The file the program produces can't be used as a regular `hosts` file, it must be used with a system that supports the `AdBlock` format.

## How to use with AdGuard Home
### Pre-built filter
The filter file is generated every six hours and is available for download [here](https://henrikwidlund.github.io/hostsparser/filter.txt).

### Adding the filters via UI
![Adding the filter](https://user-images.githubusercontent.com/4659350/129190970-bf26b383-b28d-4783-882b-372a9fe3afb8.gif)
1. Make sure that `AdGuard DNS filter` (or the custom `AdBlock` formatted file referenced when running the program) is enabled in DNS blocklists for your AdGuard Home instance.
  * If the filter isn't added, scroll down to the bottom of the page and click on `Add blocklist`.
  * Select `Choose from the list`.
  * Finally select `AdGuard DNS filter` and click `Save`.
2. Copy the link to the [Pre-built filter](#pre-built-filter) and add it to your DNS blocklists as a custom list in your AdGuard Home instance by repeating the instructions in step 1, except this time, choose `Add a custom list` instead of `Choose from the list`. In the dialog that appears, enter a name of your choosing and the URL to it. Click on `Save`.

### Adding the filters via YAML
Open and edit the `AdGuardHome.yaml` file, scroll down to the section `filters`.
1. Make sure that the `AdGuard DNS filter` is enabled (or the custom `AdBlock` formatted file referenced when running the program)
```yaml
filters:
- enabled: true
  url: https://adguardteam.github.io/AdGuardSDNSFilter/Filters/filter.txt
  name: AdGuard DNS filter
  id: 1
```
2. Add the [Pre-built filter](#pre-built-filter), replace the `id` value with [Unix Time](https://en.wikipedia.org/wiki/Unix_time).
```yaml
filters:
...
- enabled: true
  url: https://henrikwidlund.github.io/hostsparser/filter.txt
  name: HostsParser
  id: 1621690654
...
```
3. Restart the service.

Please refer to the [AdGuard Home Wiki](https://github.com/AdguardTeam/AdGuardHome/wiki) for further details on DNS blocklists.

**Note** If you've generated your own file, the [`Pre-built filter`](#pre-built-filter) link should be replaced by the address to where you host your generated file.

## Building from source
### Prerequisites
[dotnet 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0).

Run the following from the directory you cloned the repository to:

Linux/macOS
```sh
./build.sh
```
Windows
```cmd
build.cmd
```
The built files will be put in the `artifacts` directory, in the root of the repository.

## Running
### Prerequisites
1. [dotnet 6 runtime](https://dotnet.microsoft.com/download/dotnet/6.0).
2. Downloaded binaries or binaries built from source.

Run the following (if you built from source, this will be in `artifacts` directory, in the root of the repository):
```sh
dotnet HostsParser.dll
```

The program creates the `filter.txt` file in the same directory.

## Docker
You can build and run the program with Docker.

### Build
```sh
docker build ./src/HostsParser
```
### Run
#### Docker Hub
Images are available on [Docker Hub](https://hub.docker.com/r/henrikwidlund/hostsparser).
```sh
docker pull henrikwidlund/hostsparser \
    && docker create --name hostsparser henrikwidlund/hostsparser \
    && docker start hostsparser \
    && docker wait hostsparser \
    && docker cp hostsparser:/app/filter.txt . \
    && docker rm -f hostsparser
```
The `filter.txt` file will be put into the current directory.

#### Run from source
If you'd rather build and run from source, execute the following from the repository root:
```sh
IMAGE_ID=$(docker build ./src/HostsParser -q -t 'hostsparser') \
    && docker create --name hostsparser $IMAGE_ID \
    && docker start hostsparser \
    && docker wait hostsparser \
    && docker cp hostsparser:/app/filter.txt . \
    && docker rm -f hostsparser
```
The `filter.txt` file will be put into the repository root.

## Configuration
You may adjust the configuration of the application by modifying the `appsettings.json` file.

| Property | Type | Required | Description |
|---|---|---|---|
|[`HostsBased`](#hostsbased)|`object`|`true`|Settings used for processing a hosts formatted source.|
|[`AdBlockBased`](#adblockbased)|`object`|`true`|Settings used for processing a AdBlock formatted source.|
|`ExtraFiltering`|`bool`|`true`|Setting to indicate if extra filtering should be performed.<br>If `true`, the program will check each element in the result against each other and remove any entry that would be blocked by a more general entry.|
|`MultiPassFilter`|`bool`|`true`|If set to `true` the final results will be scanned multiple times until no duplicates are found. Default behaviour assumes duplicates are removed after one iteration.|
|`HeaderLines`|`string[]`|`true`|Defines a set of lines that will be inserted at the top of the generated file, for example copyright.|
|`KnownBadHosts`|`string[]`|`true`|Array of unwanted hosts. These entries will be added to the result if they're not covered by the `AdBlockBased` entries.<br>You can also add generalized hosts to reduce the number of entries in the final results.<br>For example: `HostsBased` results might contain `a.baddomain.com` and `b.baddomain.com`, adding `baddomain.com` will remove the sub domain entries and block `baddomain.com` and all of its subdomains.|

### <a name="hostsbased"></a>`HostsBased`
| Property | Type | Required | Description |
|---|---|---|---|
|`SourceUri`|`Uri`|`true`|URI to the hosts based file|
|`SkipLines`|`string[]`|`true`|Array of strings that, if present in the result from `SourceUri` will be filtered out.|

### <a name="adblockbased"></a>`AdBlockBased`
| Property | Type | Required | Description |
|---|---|---|---|
|`SourceUri`|`Uri`|`true`|URI to the AdBlock based file|

## Licenses
- [License](LICENSE)
- [StevenBlack/hosts](https://github.com/StevenBlack/hosts/blob/master/license.txt)
- [AdGuard DNS Filter](https://github.com/AdguardTeam/AdGuardSDNSFilter/blob/master/LICENSE)
