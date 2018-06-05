FROM microsoft/dotnet

WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet build

ENTRYPOINT [ "dotnet", "run" ]