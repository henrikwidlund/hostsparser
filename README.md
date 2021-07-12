# HostsParser
[![Build/Publish](https://github.com/henrikwidlund/HostsParser/actions/workflows/publish-hosts.yml/badge.svg)](https://github.com/henrikwidlund/HostsParser/actions/workflows/publish-hosts.yml)

Converts [StevenBlack/hosts](https://github.com/StevenBlack/hosts) with fakenews, gambling and porn extensions into the adblock format, optimized for [AdGuard Home](https://github.com/AdguardTeam/AdGuardHome). It also removes duplicates, hosts that are already blocked by [AdGuard DNS Filter](https://github.com/AdguardTeam/AdGuardSDNSFilter) and most comments that are used to indicate different sections in the source.

## Building
*You'll need the [dotnet 6 SDK](https://dotnet.microsoft.com/download).*

Run `dotnet build --configuration Release` from the directory you cloned the repository to.

## Running
*You'll need the [dotnet 6 runtime](https://dotnet.microsoft.com/download).*

Run `dotnet HostsParser.dll`. Program creates a `hosts` file in the same directory.

## Licenses
- [License](LICENSE)
- [StevenBlack/hosts](https://github.com/StevenBlack/hosts/blob/master/license.txt)
- [AdGuard DNS Filter](https://github.com/AdguardTeam/AdGuardSDNSFilter/blob/master/LICENSE)
