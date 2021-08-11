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

**Note** The file the program prodces can't be used as a regular `hosts` file, it must be used with a system that supports the `AdBlock` format.

## How to use with AdGuard Home
### Pre-built filter
The filter file is generated every six hours and is available for download [here](https://henrikwidlund.github.io/hostsparser/filter.txt).

### Adding the filter
1. Make sure that `AdGuard DNS filter` (or the custom `AdBlock` formatted file referenced when running the program) is active in DNS blocklists for your AdGuard Home instance.
![image](https://user-images.githubusercontent.com/4659350/129019696-98c7549a-b0ba-49a3-abf8-154b2e8fa762.png)
If it's not active, scroll down to the bottom of the page and click on `Add blocklist`<br><img width="345" src="https://user-images.githubusercontent.com/4659350/129022788-286b3a8f-d88b-404b-996e-27c69226a977.png"><br>
Select `Choose from the list`<br>
<img width="487" src="https://user-images.githubusercontent.com/4659350/129023895-81ec866e-05e7-4519-ba00-2f181ab20983.png"><br>And finally select `AdGuard DNS filter` and click `Save`<br><img width="487" src="https://user-images.githubusercontent.com/4659350/129022979-b0f8b76c-2ed7-43fb-9e28-a668e693ddd2.png">

2. Copy the link to the [Pre-built filter](#pre-built-filter) and add it to your DNS blocklists as a custom list in your AdGuard Home instance by repeating the previous step, except choose `Add a custom list` instead of `Choose from the list`. In the dialog that appears, enter a name of your choosing and the URL as instructed. Click on `Save`.<img width="487" src="https://user-images.githubusercontent.com/4659350/129023436-be65ff8a-acc6-47ef-ba05-23074f96fd73.png">

Please refer to the [AdGuard Home Wiki](https://github.com/AdguardTeam/AdGuardHome/wiki) for further details on DNS blocklists.

**Note** If you've generated your own file, the [`Pre-built filter`](#pre-built-filter) link should be replaced by the address to where you host your generated file.

## Building from source
### Prerequisites
[dotnet 6 SDK](https://dotnet.microsoft.com/download).

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
1. [dotnet 6 runtime](https://dotnet.microsoft.com/download).
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
IMAGE_ID=$(docker build ./src/HostsParser -q) \
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
|`HeaderLines`|`string[]`|`true`|Defines a set of lines that will be inserted at the top of the generated file, for example copyright.|
|`KnownBadHosts`|`string[]`|`true`|Array of unwanted hosts. These entries will be added to the result if they're not covered by the `AdBlockBased` entries.<br>You can also add generalized hosts to reduce the number of entries in final results.<br>For example: `HostsBased` results might contain `a.baddomain.com` and `b.baddomain.com`, adding `baddomain.com` will remove the sub domain entries and block `baddomain.com` and all of its subdomains.|

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
