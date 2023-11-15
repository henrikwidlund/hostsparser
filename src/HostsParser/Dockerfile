FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build
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
RUN adduser -D hostsparseru
RUN addgroup hostsparserg && addgroup hostsparseru hostsparserg
RUN touch filter.txt && chown hostsparseru:hostsparserg filter.txt
USER hostsparseru
ENTRYPOINT ["dotnet", "HostsParser.dll"]
