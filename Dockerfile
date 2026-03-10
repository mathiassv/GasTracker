FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/GasTracker.Data/GasTracker.Data.csproj src/GasTracker.Data/
COPY src/GasTracker.Web/GasTracker.Web.csproj  src/GasTracker.Web/
RUN dotnet restore src/GasTracker.Web/GasTracker.Web.csproj

COPY src/ src/
RUN dotnet publish src/GasTracker.Web/GasTracker.Web.csproj \
    -c Release -o /app/publish


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN mkdir -p /app/data

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "GasTracker.Web.dll"]
