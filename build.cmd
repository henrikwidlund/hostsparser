SET currentDirectory=%cd%
IF EXIST "%currentDirectory%\artifacts" ( RMDIR /s /q "%currentDirectory%\artifacts" )
dotnet publish "%currentDirectory%\src\HostsParser\HostsParser.csproj" -c Release -o artifacts -p:PublishAot=true