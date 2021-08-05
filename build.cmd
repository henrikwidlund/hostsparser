SET currentDirectory=%cd%
IF EXIST "%currentDirectory%\artifacts" ( RMDIR "%currentDirectory%\artifacts" )
dotnet publish "%currentDirectory%\src\HostsParser\HostsParser.csproj" -c Release -o artifacts