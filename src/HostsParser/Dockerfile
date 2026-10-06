FROM mcr.microsoft.com/dotnet/runtime-deps:10.0.12-resolute-chiseled@sha256:d7b296fb272e3089137f4397630ffc3099e131e6c655c464bf8427c3fe50b28e AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0.401-resolute-aot@sha256:37ed819a2da28a5cb2de264b64a35ce68137f04207656464b48ce3a5b7f9e32c AS build
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
