FROM mcr.microsoft.com/dotnet/runtime-deps:11.0.0-rc.2-resolute-chiseled@sha256:47ad5de0d321860bbef0ed32d652452b7d59a0de76fe52c77aebfcb112add819 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.2-resolute-aot@sha256:b93ae9a5562e11db337f31c8bace36739a03c8f1fc62c7fdd2b05d780fc57bb7 AS build
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
