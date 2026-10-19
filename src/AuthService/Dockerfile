FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY src/Shared/ECommerce.Shared.csproj src/Shared/
COPY shared/protos/ shared/protos/
COPY src/AuthService/AuthService.csproj src/AuthService/
RUN dotnet restore src/AuthService/AuthService.csproj

COPY src/Shared/ src/Shared/
COPY src/AuthService/ src/AuthService/
RUN dotnet publish src/AuthService/AuthService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5001
EXPOSE 5001
ENTRYPOINT ["dotnet", "AuthService.dll"]
