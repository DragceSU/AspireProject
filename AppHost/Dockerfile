FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore AppHost/AppHost.csproj
RUN dotnet publish AppHost/AppHost.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:5000

COPY --from=build /app/publish .

EXPOSE 5000

ENTRYPOINT ["dotnet", "AppHost.dll"]
