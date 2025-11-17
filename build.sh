if [ -d "./artifacts" ]; then
    rm -r ./artifacts
fi
dotnet publish "./src/HostsParser/HostsParser.csproj" -c Release -o artifacts