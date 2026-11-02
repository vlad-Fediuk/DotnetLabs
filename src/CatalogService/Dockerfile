FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY src/Shared/ECommerce.Shared.csproj src/Shared/
COPY shared/protos/ shared/protos/
COPY src/CatalogService/CatalogService.csproj src/CatalogService/
RUN dotnet restore src/CatalogService/CatalogService.csproj

COPY src/Shared/ src/Shared/
COPY src/CatalogService/ src/CatalogService/
RUN dotnet publish src/CatalogService/CatalogService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV Kestrel__RestPort=5002
ENV Kestrel__GrpcPort=5102
EXPOSE 5002
EXPOSE 5102
ENTRYPOINT ["dotnet", "CatalogService.dll"]
