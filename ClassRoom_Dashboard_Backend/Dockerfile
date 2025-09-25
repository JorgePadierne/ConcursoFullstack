# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ClassRoom_Dashboard_Backend/ClassRoom_Dashboard_Backend/*.csproj ClassRoom_Dashboard_Backend/ClassRoom_Dashboard_Backend/
RUN dotnet restore ClassRoom_Dashboard_Backend/ClassRoom_Dashboard_Backend/Classroom_Dashboard_Backend.csproj

# Copy everything and build
COPY . .
RUN dotnet publish ClassRoom_Dashboard_Backend/ClassRoom_Dashboard_Backend/Classroom_Dashboard_Backend.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create folder for DataProtection keys and logs
RUN mkdir -p /app/keys && mkdir -p /app/logs

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080
# Set to Production by default
ENV ASPNETCORE_ENVIRONMENT=Production

# Health probes
EXPOSE 8080

COPY --from=build /app/publish .

# Default entry
ENTRYPOINT ["dotnet", "Classroom_Dashboard_Backend.dll"]
