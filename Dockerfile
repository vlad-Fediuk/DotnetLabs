FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Shared/ECommerce.Shared.csproj src/Shared/
COPY shared/protos/ shared/protos/
COPY src/AuthService/AuthService.csproj src/AuthService/
COPY src/CatalogService/CatalogService.csproj src/CatalogService/
COPY src/OrderingService/OrderingService.csproj src/OrderingService/
COPY src/ApiGateway/ApiGateway.csproj src/ApiGateway/

RUN dotnet restore src/ApiGateway/ApiGateway.csproj && \
    dotnet restore src/AuthService/AuthService.csproj && \
    dotnet restore src/CatalogService/CatalogService.csproj && \
    dotnet restore src/OrderingService/OrderingService.csproj

COPY src/ src/

RUN dotnet publish src/AuthService/AuthService.csproj -c Release -o /app/auth /p:UseAppHost=false && \
    dotnet publish src/CatalogService/CatalogService.csproj -c Release -o /app/catalog /p:UseAppHost=false && \
    dotnet publish src/OrderingService/OrderingService.csproj -c Release -o /app/ordering /p:UseAppHost=false && \
    dotnet publish src/ApiGateway/ApiGateway.csproj -c Release -o /app/gateway /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/auth ./auth
COPY --from=build /app/catalog ./catalog
COPY --from=build /app/ordering ./ordering
COPY --from=build /app/gateway ./gateway

COPY docker/entrypoint.sh ./entrypoint.sh
RUN chmod +x ./entrypoint.sh

ENV Services__Auth="http://127.0.0.1:5001"
ENV Services__Catalog="http://127.0.0.1:5002"
ENV Services__Ordering="http://127.0.0.1:5003"
ENV Services__CatalogGrpc="http://127.0.0.1:5102"
ENV Kestrel__RestPort="5002"
ENV Kestrel__GrpcPort="5102"

EXPOSE 10000

ENTRYPOINT ["./entrypoint.sh"]
