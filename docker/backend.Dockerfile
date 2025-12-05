FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/src/HybridCloudWorkloads.API/HybridCloudWorkloads.API.csproj ./HybridCloudWorkloads.API/
COPY backend/src/HybridCloudWorkloads.Core/HybridCloudWorkloads.Core.csproj ./HybridCloudWorkloads.Core/
COPY backend/src/HybridCloudWorkloads.Infrastructure/HybridCloudWorkloads.Infrastructure.csproj ./HybridCloudWorkloads.Infrastructure/

RUN dotnet restore HybridCloudWorkloads.API/HybridCloudWorkloads.API.csproj

COPY backend/src/ .
WORKDIR /src/HybridCloudWorkloads.API
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HybridCloudWorkloads.API.dll"]