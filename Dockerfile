FROM mcr.microsoft.com/dotnet/core/aspnet:2.1

COPY src/discordbot/bin/Release/netcoreapp2.1/publish/ App/

WORKDIR /App

ENTRYPOINT [ "dotnet", "discordbot.dll" ]
