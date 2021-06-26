# HostsParser
[![Build](https://github.com/henrikwidlund/HostsParser/actions/workflows/build.yml/badge.svg)](https://github.com/henrikwidlund/HostsParser/actions/workflows/build.yml)
[![Publish](https://github.com/henrikwidlund/HostsParser/actions/workflows/ci.yml/badge.svg)](https://github.com/henrikwidlund/HostsParser/actions/workflows/ci.yml)

Converts [StevenBlack/hosts](https://github.com/StevenBlack/hosts) with fakenews, gambling and porn extensions into a format that works with [AdGuard Home](https://github.com/AdguardTeam/AdGuardHome). It also removes duplicates, hosts that are already blocked by [AdGuard DNS Filter](https://github.com/AdguardTeam/AdGuardSDNSFilter) and most comments used to indicate different sections in the source.

## Building
*You'll need the [dotnet 5 SDK](https://dotnet.microsoft.com/download).*

Run `dotnet build --configuration Release` from the directory you cloned the repository to.

## Running
*You'll need the [dotnet 5 runtime](https://dotnet.microsoft.com/download).*

Run `dotnet HostsParser.dll`. Program creates a `hosts` file in the same directory.

## Licenses
- [License](LICENSE)
- [StevenBlack/hosts](https://github.com/StevenBlack/hosts/blob/master/license.txt)
- [AdGuard DNS Filter](https://github.com/AdguardTeam/AdGuardSDNSFilter/blob/master/LICENSE)
