FROM mcr.microsoft.com/dotnet/core/aspnet:5.0

COPY src/discordbot/bin/Release/net5.0/publish/ App/

WORKDIR /App

ENTRYPOINT [ "dotnet", "discordbot.dll" ]
