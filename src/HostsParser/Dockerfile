﻿FROM mcr.microsoft.com/dotnet/runtime-deps:9.0-noble-chiseled AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0-noble AS build
WORKDIR /src
COPY "HostsParser.csproj" .
RUN dotnet restore "HostsParser.csproj"
COPY . .

FROM build AS publish
RUN dotnet publish "HostsParser.csproj" -c Release -o /app/publish /p:UseAppHost=true

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./HostsParser"]
