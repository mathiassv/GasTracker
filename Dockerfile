FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Packages.props ./
COPY src/GasTracker.Data/GasTracker.Data.csproj src/GasTracker.Data/
COPY src/GasTracker.Web/GasTracker.Web.csproj  src/GasTracker.Web/
RUN dotnet restore src/GasTracker.Web/GasTracker.Web.csproj

COPY src/ src/
RUN dotnet publish src/GasTracker.Web/GasTracker.Web.csproj \
    -c Release -o /app/publish


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl is only used by the HEALTHCHECK below
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/data \
    && chown app:app /app/data

COPY --from=build /app/publish .
COPY --chmod=755 docker-entrypoint.sh /usr/local/bin/docker-entrypoint.sh

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl -fsS http://localhost:8080/healthz || exit 1

# The entrypoint fixes ownership of the mounted /app/data volume, then runs the app as the
# image's built-in non-root user `app` (UID 1654).
ENTRYPOINT ["docker-entrypoint.sh"]
CMD ["dotnet", "GasTracker.Web.dll"]
