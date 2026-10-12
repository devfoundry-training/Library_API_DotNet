# Library API (ASP.NET Core on .NET 10), built in two stages.
#   docker build -t library-api-dotnet .

# Stage 1: restore and publish with the full SDK.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Restore first, on its own layer, so a code change does not download the packages again.
COPY src/Library.Api/Library.Api.csproj src/Library.Api/
RUN dotnet restore src/Library.Api/Library.Api.csproj
COPY src/ src/
RUN dotnet publish src/Library.Api/Library.Api.csproj -c Release -o /app --no-restore

# Stage 2: the ASP.NET Core runtime only (includes ICU and time zone data).
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
# A folder the non-root user can write to, for the SQLite default.
RUN mkdir /app/data && chown "$APP_UID" /app/data
# The app listens on PORT, so clear the image's own default of 8080.
ENV PORT=8000 \
    DATABASE_URL=sqlite:///data/library.db \
    TZ=Europe/London \
    ASPNETCORE_HTTP_PORTS=
EXPOSE 8000
# APP_UID is the non-root "app" user that the official .NET images provide.
USER $APP_UID
ENTRYPOINT ["dotnet", "Library.Api.dll"]
