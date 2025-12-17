# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WebApi.Service/WebApi.Service.csproj", "WebApi.Service/"]
COPY ["MessageContracts/MessageContracts.csproj", "MessageContracts/"]
COPY ["Messaging/Messaging.csproj", "Messaging/"]
RUN dotnet restore "WebApi.Service/WebApi.Service.csproj"
COPY . .
WORKDIR "/src/WebApi.Service"
RUN dotnet build "WebApi.Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "WebApi.Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "WebApi.Service.dll"]
