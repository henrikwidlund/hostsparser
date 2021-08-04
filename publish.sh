if [ -d "./publish" ]
then
    rm -r ./publish
fi
dotnet publish "./src/HostsParser/HostsParser.csproj" -c Release -o publish