# HostsParser

[![Publish Filter](https://img.shields.io/github/workflow/status/henrikwidlund/hostsparser/Publish%20Filter?label=Publish%20Filter&logo=github)](https://github.com/henrikwidlund/hostsparser/actions/workflows/publish-filter.yml)
[![CI](https://img.shields.io/github/workflow/status/henrikwidlund/hostsparser/Build%20and%20Test?label=CI&logo=github)](https://github.com/henrikwidlund/hostsparser/actions/workflows/ci.yml)
[![CodeQL](https://img.shields.io/github/workflow/status/henrikwidlund/hostsparser/CodeQL?label=CodeQL&logo=github)](https://github.com/henrikwidlund/hostsparser/actions/workflows/codeql-analysis.yml)
[![Docker](https://img.shields.io/github/workflow/status/henrikwidlund/hostsparser/Docker?label=Docker&logo=docker)](https://github.com/henrikwidlund/hostsparser/actions/workflows/docker.yml)
[![codecov.io](https://img.shields.io/codecov/c/gh/henrikwidlund/hostsparser?label=codecov&logo=codecov)](https://codecov.io/gh/henrikwidlund/hostsparser)

Tool for producing an `AdBlock` formatted file from different sources. `hosts` and `AdBlock` based formats are supported for the sources and you can specify if the contents in the sources should be excluded or included in the result.
It also removes duplicates, comments as well as hosts as well as hosts that would otherwise be blocked by a more general entry.

By default [StevenBlack/hosts](https://github.com/StevenBlack/hosts) 
[with fakenews, gambling and porn extensions](https://raw.githubusercontent.com/StevenBlack/hosts/master/alternates/fakenews-gambling-porn/hosts)
is processed to exclude entries already covered by the [AdGuard DNS Filter](https://github.com/AdguardTeam/AdGuardSDNSFilter)
[file](https://adguardteam.github.io/AdGuardSDNSFilter/Filters/filter.txt).

**Note** The file the program produces can't be used as a regular `hosts` file, it must be used with a system that supports the `AdBlock` format.

## How to use with AdGuard Home
### Pre-built filters
The filter files are generated every six hours and are available for download in the table below. You are welcome to create a feature request should you want more pre-built filters.

| Filter                                                        | Link                                                                                  |
|---------------------------------------------------------------|---------------------------------------------------------------------------------------|
| `Unified hosts` = `adware` + `malware`                        | [link](https://henrikwidlund.github.io/hostsparser/adware-malware.txt)                |
| `Unified hosts` + `fakenews`                                  | [link](https://henrikwidlund.github.io/hostsparser/fakenews.txt)                      |
| `Unified hosts` + `fakenews` + `gambling`                     | [link](https://henrikwidlund.github.io/hostsparser/fakenews-gambling.txt)             |
| `Unified hosts` + `fakenews` + `gambling` + `porn`            | [link](https://henrikwidlund.github.io/hostsparser/filter.txt)                        |
| `Unified hosts` + `fakenews` + `gambling` + `porn` + `social` | [link](https://henrikwidlund.github.io/hostsparser/fakenews-gambling-porn-social.txt) |
| `Unified hosts` + `fakenews` + `gambling` + `social`          | [link](https://henrikwidlund.github.io/hostsparser/fakenews-gambling-social.txt)      |
| `Unified hosts` + `fakenews` + `porn`                         | [link](https://henrikwidlund.github.io/hostsparser/fakenews-porn.txt)                 |
| `Unified hosts` + `fakenews` + `porn` + `social`              | [link](https://henrikwidlund.github.io/hostsparser/fakenews-porn-social.txt)          |
| `Unified hosts` + `fakenews` + `social`                       | [link](https://henrikwidlund.github.io/hostsparser/fakenews-social.txt)               |
| `Unified hosts` + `gambling`                                  | [link](https://henrikwidlund.github.io/hostsparser/gambling.txt)                      |
| `Unified hosts` + `gambling` + `porn`                         | [link](https://henrikwidlund.github.io/hostsparser/gambling-porn.txt)                 |
| `Unified hosts` + `gambling` + `porn` + `social`              | [link](https://henrikwidlund.github.io/hostsparser/gambling-porn-social.txt)          |
| `Unified hosts` + `gambling` + `social`                       | [link](https://henrikwidlund.github.io/hostsparser/gambling-social.txt)               |
| `Unified hosts` + `porn`                                      | [link](https://henrikwidlund.github.io/hostsparser/porn.txt)                          |
| `Unified hosts` + `porn` + `social`                           | [link](https://henrikwidlund.github.io/hostsparser/porn-social.txt)                   |
| `Unified hosts` + `social`                                    | [link](https://henrikwidlund.github.io/hostsparser/social.txt)                        |

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

## Building from source code
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
2. Downloaded binaries or binaries built from source code.

Run the following (if you built from source code, this will be in `artifacts` directory, in the root of the repository):
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

#### Run from source code
If you'd rather build and run from source code, execute the following from the repository root:
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

| Property              | Type       | Required | Description                                                                                                                                                                                                                                                                                                                                                                                                                     |
|-----------------------|------------|----------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [`Filters`](#filters) | `object`   | `true`   | Settings used for processing hosts formatted sources.                                                                                                                                                                                                                                                                                                                                                                           |
| `ExtraFiltering`      | `bool`     | `true`   | Setting to indicate if extra filtering should be performed.<br>If `true`, the program will check each element in the result against each other and remove any entry that would be blocked by a more general entry.                                                                                                                                                                                                              |
| `MultiPassFilter`     | `bool`     | `true`   | If set to `true` the final results will be scanned multiple times until no duplicates are found. Default behaviour assumes duplicates are removed after one iteration.                                                                                                                                                                                                                                                          |
| `HeaderLines`         | `string[]` | `true`   | Defines a set of lines that will be inserted at the top of the generated file, for example copyright.                                                                                                                                                                                                                                                                                                                           |
| `KnownBadHosts`       | `string[]` | `true`   | Array of unwanted hosts. These entries will be added to the result if they're not covered by the `AdBlockBased` entries.<br>You can also add generalized hosts to reduce the number of entries in the final results.<br>For example: `HostsBased` results might contain `a.baddomain.com` and `b.baddomain.com`, adding `baddomain.com` will remove the sub domain entries and block `baddomain.com` and all of its subdomains. |
| `OutputFileName`      | `string`   | `false`  | Defines the name of the file produced by the program. Defaults to `filter.txt`.                                                                                                                                                                                                                                                                                                                                                 |

### <a name="filters"></a>`Filters`
| Property                 | Type       | Required | Description                                                                          |
|--------------------------|------------|----------|--------------------------------------------------------------------------------------|
| [`Sources`](#sourceitem) | `object[]` | `true`   | Array of [`SourceItem`](#sourceitem) used for fetching and processing filters.       |
| `SkipLines`              | `string[]` | `true`   | Array of strings that, if present in the result from `Sources` will be filtered out. |

### <a name="sourceitem"></a>`SourceItem`
| Property       | Type     | Required | Description                                                                                                        |
|----------------|----------|----------|--------------------------------------------------------------------------------------------------------------------|
| `Uri`          | `Uri`    | `true`   | The Uri to fetch data from.                                                                                        |
| `Prefix`       | `string` | `false`  | Prefix used in the source, for example 127.0.0.1 or 0.0.0.0.                                                       |
| `Format`       | `enum`   | `true`   | The format of the source. Possible values `Hosts`, `AdBlock`.                                                      |
| `SourceAction` | `enum`   | `true`   | Defines if the data from the source should be combined or excluded. Possible values `Combine`, `ExternalCoverage`. |

## Licenses
- [License](LICENSE)
- [StevenBlack/hosts](https://github.com/StevenBlack/hosts/blob/master/license.txt)
- [AdGuard DNS Filter](https://github.com/AdguardTeam/AdGuardSDNSFilter/blob/master/LICENSE)
