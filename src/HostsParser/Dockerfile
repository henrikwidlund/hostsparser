FROM mcr.microsoft.com/dotnet/runtime-deps:10.0.11-noble-chiseled@sha256:afd8faf9aca00ee69b44b33631665e654e2423e143b6362dffe52c006cd7c4e6 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0.400-noble-aot@sha256:74edbceabfc6d1a7e4a5764be034fd744a07f60d26703232e74f6a5edc04e8ba AS build
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
