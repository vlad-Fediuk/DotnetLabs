FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY src/Shared/ECommerce.Shared.csproj src/Shared/
COPY shared/protos/ shared/protos/
COPY src/Monolith/ECommerce.Monolith.csproj src/Monolith/
RUN dotnet restore src/Monolith/ECommerce.Monolith.csproj

COPY src/Shared/ src/Shared/
COPY src/Monolith/ src/Monolith/
RUN dotnet publish src/Monolith/ECommerce.Monolith.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5100
EXPOSE 5100
ENTRYPOINT ["dotnet", "ECommerce.Monolith.dll"]
