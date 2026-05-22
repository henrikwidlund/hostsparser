# 10.0.8
FROM mcr.microsoft.com/dotnet/runtime-deps@sha256:70c6a58170a1c987724748d5dd185d7cc75d2d9017c80348a27aa95d3ea45c67 AS base
WORKDIR /app

# 10.0.300
FROM mcr.microsoft.com/dotnet/sdk@sha256:76ce51d361bd5be4b531d07c9fef64e8d6c6d17c2dfc0e18137ca21e1604b864 AS build
WORKDIR /src
COPY "HostsParser.csproj" .
RUN dotnet restore "HostsParser.csproj" --locked-mode
COPY . .

FROM build AS publish
RUN dotnet publish "HostsParser.csproj" -c Release -o /app/publish /p:UseAppHost=true

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./HostsParser"]
