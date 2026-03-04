FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Nexopostal.Reparto/Nexopostal.Reparto.csproj", "Nexopostal.Reparto/"]
RUN dotnet restore "Nexopostal.Reparto/Nexopostal.Reparto.csproj"
COPY . .
WORKDIR "/src/Nexopostal.Reparto"
RUN dotnet build "./Nexopostal.Reparto.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Nexopostal.Reparto.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "Nexopostal.Reparto.dll"]
