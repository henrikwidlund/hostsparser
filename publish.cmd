SET currentDirectory=%cd%
IF EXIST "%currentDirectory%\publish" ( RMDIR "%currentDirectory%\publish" )
dotnet publish "%currentDirectory%\src\HostsParser\HostsParser.csproj" -c Release -o publish