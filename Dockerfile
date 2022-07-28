FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ThingsAPI.csproj", "."]
RUN dotnet restore "./ThingsAPI.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "ThingsAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ThingsAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ThingsAPI.dll"]