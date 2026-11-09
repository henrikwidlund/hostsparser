FROM mcr.microsoft.com/dotnet/runtime-deps:11.0.0-rc.3-resolute-chiseled@sha256:e7f17490b7d668dbcba8671ed287e3e023bb385e8aa7851f3c803acbd3aff61e AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.3-resolute-aot@sha256:5d98cd6acf126805920c9e723a7abe3b121cd386229fb06bcacd916f27192024 AS build
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
