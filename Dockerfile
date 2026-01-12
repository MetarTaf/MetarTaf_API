FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopiér solution og alle projektfiler
COPY MetarTaf_API.sln .
COPY Api/*.csproj ./Api/
COPY Domain/*.csproj ./Domain/
COPY Application/*.csproj ./Application/
COPY Infrastructure/*.csproj ./Infrastructure/
COPY Tests/*.csproj ./Tests/

# Restore dependencies
RUN dotnet restore

# Kopiér resten af koden
COPY . .

# Build og publish Api-projektet
RUN dotnet publish Api -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Api.dll"]
