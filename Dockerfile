FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and project files first (layer cache optimization)
COPY TransactionCompliance.sln .
COPY Directory.Build.props .
COPY src/Core/Core.csproj src/Core/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
COPY src/Api/Api.csproj src/Api/
COPY tests/Core.Tests/Core.Tests.csproj tests/Core.Tests/
COPY tests/Api.Tests/Api.Tests.csproj tests/Api.Tests/

RUN dotnet restore TransactionCompliance.sln

# Copy all source
COPY . .

RUN dotnet build TransactionCompliance.sln --no-restore -c Release

RUN dotnet publish src/Api/Api.csproj --no-build -c Release -o src/Api/bin/Release/net8.0/publish/

# Test stage — runs all tests
FROM build AS test
RUN dotnet test TransactionCompliance.sln --no-build -c Release \
    --logger "trx;LogFileName=results.trx"

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/src/Api/bin/Release/net8.0/publish/ .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Api.dll"]
