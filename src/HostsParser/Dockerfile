FROM mcr.microsoft.com/dotnet/runtime:6.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
WORKDIR /src
COPY "HostsParser.csproj" .
RUN dotnet restore "HostsParser.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "HostsParser.csproj" -c Release -o /app/build

ARG ASSEMBLY_VERSION=1.0.0
FROM build AS publish
RUN dotnet publish "HostsParser.csproj" -c Release -o /app/publish /p:Version="$ASSEMBLY_VERSION"

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HostsParser.dll"]
