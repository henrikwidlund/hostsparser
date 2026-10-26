FROM mcr.microsoft.com/dotnet/runtime-deps:11.0.0-rc.3-resolute-chiseled@sha256:0a70ecd3350d11f4a5377be3a9e44372ef28281ee162b71f498ed69fea901745 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.3-resolute-aot@sha256:11122a6e67b587afb3f6969b2e1e72eb71f36eba974da78b10eec6e2d0706057 AS build
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
