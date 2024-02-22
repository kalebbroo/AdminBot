# Use the official Microsoft .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Assuming your .csproj file is in the AdminBot directory directly under the project root
COPY *.csproj ./AdminBot/
RUN dotnet restore "./AdminBot/AdminBot.csproj"

# Copy the rest of the source code
COPY . ./AdminBot/

# Publish the application to the out directory
RUN dotnet publish "./AdminBot/AdminBot.csproj" -c Release -o out

# Build the runtime image using the official Microsoft .NET runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

# Install Opus and libsodium
RUN apt-get update && apt-get install -y libopus0 ffmpeg libopus-dev libsodium23 && rm -rf /var/lib/apt/lists/*

COPY --from=build-env /app/out .

ENTRYPOINT ["dotnet", "AdminBot.dll"]
