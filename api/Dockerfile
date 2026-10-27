# Se construye desde la raíz del repositorio (docker-compose.yml): necesita las fotos de demostración del frontend.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY api/src/Pujalo.Api/Pujalo.Api.csproj api/src/Pujalo.Api/
RUN dotnet restore api/src/Pujalo.Api/Pujalo.Api.csproj
COPY api/src/Pujalo.Api/ api/src/Pujalo.Api/
RUN dotnet publish api/src/Pujalo.Api/Pujalo.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
COPY frontend/src/assets/fotos /app/demo-fotos
ENV Demo__CarpetaFotos=/app/demo-fotos
EXPOSE 8080
ENTRYPOINT ["dotnet", "Pujalo.Api.dll"]
