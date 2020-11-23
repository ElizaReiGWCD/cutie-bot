FROM mcr.microsoft.com/dotnet/sdk:5.0

COPY . App/

WORKDIR /App

RUN dotnet publish -c "Release"

WORKDIR /App/src/discordbot/bin/Release/net5.0/publish

ENTRYPOINT [ "dotnet", "discordbot.dll" ]
