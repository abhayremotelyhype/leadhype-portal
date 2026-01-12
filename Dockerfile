# Use official .NET 8 SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY Api/Api.csproj ./Api/
RUN dotnet restore ./Api/Api.csproj

# Copy everything else and build
COPY Api/ ./Api/
WORKDIR /app/Api
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/Api/out .

# Copy migrations
COPY Api/Core/Database/Migrations ./Core/Database/Migrations

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Start the app
ENTRYPOINT ["dotnet", "Api.dll"]
