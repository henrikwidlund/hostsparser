FROM mcr.microsoft.com/dotnet/runtime-deps:11.0.0-resolute-chiseled@sha256:afc4f04086a96c8407d912ef30d9a3c387111a362acd76e46a5c7a18702c39e4 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:11.0.100-resolute-aot@sha256:b9ddcead074e2fc9338c3427f8f0e212e2cd4783ea26c8853ea412221518d1af AS build
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
