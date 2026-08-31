FROM mcr.microsoft.com/dotnet/runtime-deps:10.0.11-noble-chiseled@sha256:afd8faf9aca00ee69b44b33631665e654e2423e143b6362dffe52c006cd7c4e6 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0.400-noble-aot@sha256:2ef30f119199e148cb35fd954ca61eddcf02f2996059f782899d318451ff4967 AS build
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
