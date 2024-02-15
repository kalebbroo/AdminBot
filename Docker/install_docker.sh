#!/bin/bash

# Change directory to the script's location
cd "$(dirname "$0")"
echo "Current directory: $(pwd)"

# Navigate up to the project root directory where the .env file is
cd ..

# Set the path of the .env file relative to this new current directory
export ENV_FILE=.env
echo "Locating .env file at: $(pwd)/$ENV_FILE"

# Check if .env file exists
if [ -f $ENV_FILE ] 
then

  echo "Found .env file!"


  # run docker compose 
  echo Running Docker Compose...
  docker compose -p adminbot --env-file "$ENV_FILE" -f Docker/docker-compose.yml up --build
  
  exit 0

else

  echo "Error: .env file not found"
  read -p "Please make sure it exists and try again"
  exit 1

fi
