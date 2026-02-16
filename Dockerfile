# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy all project files to their respective locations
COPY ["FlightOptimizer.API/FlightOptimizer.API.csproj", "FlightOptimizer.API/"]
COPY ["FlightOptimizer.Core/FlightOptimizer.Core.csproj", "FlightOptimizer.Core/"]
COPY ["FlightOptimizer.Infrastructure/FlightOptimizer.Infrastructure.csproj", "FlightOptimizer.Infrastructure/"]

# Restore dependencies for the API project
RUN dotnet restore "FlightOptimizer.API/FlightOptimizer.API.csproj"

# Copy the rest of the source code
COPY . .

# Build and Publish
WORKDIR "/src/FlightOptimizer.API"
RUN dotnet publish -c Release -o /app/publish

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Configure for Render
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FlightOptimizer.API.dll"]
