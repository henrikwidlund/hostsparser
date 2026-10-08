FROM mcr.microsoft.com/dotnet/runtime-deps:11.0.0-rc.1-resolute-chiseled@sha256:d5ac1fe29eb973331581ab9737688e646f437894ae38a130d4652542ed19964b AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.1-resolute-aot@sha256:f7b00e0a7dedf120a340d093b1870dbe73ddf6f53284e688d0281a21dcfdcae7 AS build
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["src/HostsParser/HostsParser.csproj", "."]
RUN dotnet restore "HostsParser.csproj" --locked-mode
COPY ["src/HostsParser", "."]

FROM build AS publish
RUN dotnet publish "HostsParser.csproj" -c Release -o /app/publish /p:UseAppHost=true

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./HostsParser"]
