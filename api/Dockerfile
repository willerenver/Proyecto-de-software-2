# Se construye desde la raíz del repositorio (docker-compose.yml): necesita las fotos de demostración del frontend.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY api/src/ElMejorPostor.Api/ElMejorPostor.Api.csproj api/src/ElMejorPostor.Api/
RUN dotnet restore api/src/ElMejorPostor.Api/ElMejorPostor.Api.csproj
COPY api/src/ElMejorPostor.Api/ api/src/ElMejorPostor.Api/
RUN dotnet publish api/src/ElMejorPostor.Api/ElMejorPostor.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
COPY frontend/src/assets/fotos /app/demo-fotos
ENV Demo__CarpetaFotos=/app/demo-fotos
EXPOSE 8080
ENTRYPOINT ["dotnet", "ElMejorPostor.Api.dll"]
