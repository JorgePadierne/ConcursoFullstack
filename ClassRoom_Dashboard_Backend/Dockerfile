# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy whole repository
COPY . .

# Locate the csproj dynamically (case-sensitive in Linux) and restore/publish
RUN set -e \
    && echo "Searching for csproj..." \
    && CSProjPath=$(find /src -type f -name 'Classroom_Dashboard_Backend.csproj' -print -quit) \
    && echo "Found: $CSProjPath" \
    && if [ -z "$CSProjPath" ]; then echo "ERROR: .csproj not found" && exit 1; fi \
    && dotnet restore "$CSProjPath" \
    && dotnet publish "$CSProjPath" -c Release -o /app/publish /p:UseAppHost=false

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
