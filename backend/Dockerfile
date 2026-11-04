# Build: the SDK image restores (cached while Api.csproj is unchanged) and publishes a Release build.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Api/Api.csproj Api/
RUN dotnet restore Api/Api.csproj
COPY Api/ Api/
RUN dotnet publish Api/Api.csproj -c Release -o /app --no-restore

# Run: the smaller ASP.NET runtime image, as the image's built-in non-root user.
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
# Render tells the app which port to listen on through $PORT (default 10000); 8080 when run elsewhere.
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT:-8080} exec dotnet Api.dll"]
