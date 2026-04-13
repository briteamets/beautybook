FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY beautybook.sln ./
COPY WebApp/WebApp.csproj WebApp/
COPY App.DAL.EF/App.DAL.EF.csproj App.DAL.EF/
COPY App.DTO/App.DTO.csproj App.DTO/
COPY App.Domain/App.Domain.csproj App.Domain/
COPY App.Resources/App.Resources.csproj App.Resources/
COPY Base.Contracts/Base.Contracts.csproj Base.Contracts/
COPY Base.Domain/Base.Domain.csproj Base.Domain/
COPY Base.Helpers/Base.Helpers.csproj Base.Helpers/

RUN dotnet restore WebApp/WebApp.csproj

COPY . .
RUN dotnet publish WebApp/WebApp.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "WebApp.dll"]
