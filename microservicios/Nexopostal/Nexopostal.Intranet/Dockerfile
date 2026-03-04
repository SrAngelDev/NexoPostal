FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Nexopostal.Intranet/Nexopostal.Intranet.csproj", "Nexopostal.Intranet/"]
RUN dotnet restore "Nexopostal.Intranet/Nexopostal.Intranet.csproj"
COPY . .
WORKDIR "/src/Nexopostal.Intranet"
RUN dotnet build "./Nexopostal.Intranet.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Nexopostal.Intranet.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "Nexopostal.Intranet.dll"]
