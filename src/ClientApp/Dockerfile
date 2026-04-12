FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY App.DTO/App.DTO.csproj App.DTO/
COPY src/ClientApp/ClientApp.csproj src/ClientApp/
RUN dotnet restore src/ClientApp/ClientApp.csproj

COPY App.DTO/ App.DTO/
COPY src/ClientApp/ src/ClientApp/
RUN dotnet publish src/ClientApp/ClientApp.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ClientApp.dll"]
