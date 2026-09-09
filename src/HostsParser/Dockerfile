FROM mcr.microsoft.com/dotnet/runtime-deps:10.0.12-resolute-chiseled@sha256:93f4087fb76adb7446fd6c9d29be8b974f558bf4553b5ca09d59ad9c65164804 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0.401-resolute-aot@sha256:59f26f8464f5fb62a7bcdefcb0db0f24b5ac38ff24fee51677da3a6a02a6e2eb AS build
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
