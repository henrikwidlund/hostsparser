﻿FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-jammy-chiseled AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
WORKDIR /src
COPY "HostsParser.csproj" .
RUN dotnet restore "HostsParser.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "HostsParser.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HostsParser.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HostsParser.dll"]
