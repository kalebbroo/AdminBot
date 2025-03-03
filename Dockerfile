# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

# Copy csproj and restore dependencies
COPY *.sln .
COPY src/*.csproj ./src/
RUN dotnet restore

# Copy all files and build
COPY . .
RUN dotnet publish -c release -o /app --no-restore

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Install required packages
RUN apt-get update && apt-get install -y \
    ffmpeg \
    libopus0 \
    libopus-dev \
    libsodium23 \
    libsodium-dev \
    && rm -rf /var/lib/apt/lists/*

# Copy build output
COPY --from=build /app ./

# Copy default config files
COPY appsettings.json .
COPY .env.example .env

# Set environment variables
ENV DOTNET_ENVIRONMENT=Production
ENV ASPNETCORE_ENVIRONMENT=Production

# Create volume for logs
VOLUME /app/logs

# Health check
HEALTHCHECK --interval=30s --timeout=3s \
    CMD curl -f http://localhost/health || exit 1

# Set entry point
ENTRYPOINT ["dotnet", "DiscordBot.dll"]