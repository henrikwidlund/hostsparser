# HostsParser
[![Build/Publish](https://github.com/henrikwidlund/HostsParser/actions/workflows/publish-hosts.yml/badge.svg)](https://github.com/henrikwidlund/HostsParser/actions/workflows/publish-hosts.yml)

Converts a `hosts` ([`HostsBased`](#hostsbased)) based file into a `AdBlock` formatted file, optimized for [AdGuard Home](https://github.com/AdguardTeam/AdGuardHome).
It also removes duplicates, comments as well as hosts that are already blocked by a different `AdBlock` ([`AdBlockBased`](#adblockbased)) based file.

By default [StevenBlack/hosts](https://github.com/StevenBlack/hosts) 
[with fakenews, gambling and porn extensions](https://raw.githubusercontent.com/StevenBlack/hosts/master/alternates/fakenews-gambling-porn/hosts)
is processed to exclude entries already covered by the [AdGuard DNS Filter](https://github.com/AdguardTeam/AdGuardSDNSFilter)
[file](https://adguardteam.github.io/AdGuardSDNSFilter/Filters/filter.txt).

**Note** this can't be used as a regular hosts file, it must be used with a system that supports the `AdBlock` format.

## How to use with AdGuard Home
1. Make sure that `AdGuard DNS filter` (or the custom `AdBlock` formatted file referenced when running the program) is active in DNS blocklists for your AdGuard Home instance.
2. Copy the link to the [Pre-built filter](#pre-built-filter) and add it to your DNS blocklists as a custom list in your AdGuard Home instance.

Please refer to the [AdGuard Home](https://github.com/AdguardTeam/AdGuardHome) repository for further instructions on how to use DNS blocklists.

**Note** If you've generated your own file, the Pre-built filter link should be replaced by the address to where you host your generated file.

### Pre-built filter
The filter file is generated every six hours and is available for download [here](https://henrikwidlund.github.io/HostsParser/filter.txt).

## Building from source
### Prerequisites
[dotnet 6 SDK](https://dotnet.microsoft.com/download).

Run the following from the directory you cloned the repository to:
```sh
./publish.sh
```
or
````cmd
publish.cmd
````
The built files will be put in `./publish`

## Running
### Prerequisites
1. [dotnet 6 runtime](https://dotnet.microsoft.com/download).
2. Downloaded binaries or binaries built from source.

Run the following (if you built from source, this will be in `./publish`):
```sh
dotnet HostsParser.dll
```

The program creates the `filter.txt` file in the same directory.

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
