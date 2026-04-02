FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/ProductService/ProductService.csproj ./ProductService/
RUN dotnet restore ProductService/ProductService.csproj
COPY src/ProductService/ ./ProductService/
RUN dotnet build ProductService/ProductService.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish ProductService/ProductService.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProductService.dll"]
