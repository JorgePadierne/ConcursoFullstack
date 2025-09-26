# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["ClassRoom_Dashboard_Backend/ClassRoom_Dashboard_Backend.csproj", "ClassRoom_Dashboard_Backend/"]
RUN dotnet restore "ClassRoom_Dashboard_Backend/ClassRoom_Dashboard_Backend.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/ClassRoom_Dashboard_Backend"
RUN dotnet build "ClassRoom_Dashboard_Backend.csproj" -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish "ClassRoom_Dashboard_Backend.csproj" -c Release -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create necessary directories
RUN mkdir -p /app/keys && \
    mkdir -p /app/logs && \
    chmod -R 755 /app/keys && \
    chmod -R 755 /app/logs

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    TZ=UTC

# Expose port 8080 for Render
EXPOSE 8080

# Copy published app
COPY --from=publish /app/publish .

# Set the entrypoint
ENTRYPOINT ["dotnet", "ClassRoom_Dashboard_Backend.dll"]
