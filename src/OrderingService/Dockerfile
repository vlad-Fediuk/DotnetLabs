FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY src/Shared/ECommerce.Shared.csproj src/Shared/
COPY shared/protos/ shared/protos/
COPY src/OrderingService/OrderingService.csproj src/OrderingService/
RUN dotnet restore src/OrderingService/OrderingService.csproj

COPY src/Shared/ src/Shared/
COPY src/OrderingService/ src/OrderingService/
RUN dotnet publish src/OrderingService/OrderingService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5003
EXPOSE 5003
ENTRYPOINT ["dotnet", "OrderingService.dll"]
