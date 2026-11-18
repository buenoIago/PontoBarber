# build: restaura e publica somente o grafo da API (WebApp -> Aplicacao -> Infra -> Dominio).
FROM mcr.microsoft.com/dotnet/sdk:10.0.103 AS build

WORKDIR /src

# global.json fixa o SDK 10.0.103; props centrais definem TFM, analisadores e versoes de pacotes.
COPY global.json Directory.Build.props Directory.Packages.props ./

COPY server/src/PontoBarber.WebApp/PontoBarber.WebApp.csproj server/src/PontoBarber.WebApp/
COPY server/src/PontoBarber.Aplicacao/PontoBarber.Aplicacao.csproj server/src/PontoBarber.Aplicacao/
COPY server/src/PontoBarber.Dominio/PontoBarber.Dominio.csproj server/src/PontoBarber.Dominio/
COPY server/src/PontoBarber.Infra/PontoBarber.Infra.csproj server/src/PontoBarber.Infra/

RUN dotnet restore server/src/PontoBarber.WebApp/PontoBarber.WebApp.csproj

COPY server/src/ server/src/

RUN dotnet publish server/src/PontoBarber.WebApp/PontoBarber.WebApp.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# final: runtime .NET 10 (10.0.3, coerente com o SDK 10.0.103), Linux, sem segredos.
FROM mcr.microsoft.com/dotnet/aspnet:10.0.3 AS final

WORKDIR /app

# curl e usado pelo HEALTHCHECK; o cache do apt e removido na mesma camada.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build --chown=app:app /app/publish ./

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

USER app

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl --fail --silent --show-error --max-time 3 http://127.0.0.1:8080/health/live > /dev/null || exit 1

ENTRYPOINT ["dotnet", "PontoBarber.WebApp.dll"]
