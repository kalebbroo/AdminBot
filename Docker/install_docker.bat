@echo off
SETLOCAL EnableDelayedExpansion

REM Change directory to the script's location
cd %~dp0
echo Current directory: %cd%

REM Navigate up to the project root directory where the .env file is located
cd ..

REM Set the path to the .env file relative to this new current directory
SET ENV_FILE=.env
echo Looking for .env file at: %cd%\%ENV_FILE%

REM Check if .env file exists
IF NOT EXIST "%ENV_FILE%" (
    echo Error: .env file not found at %cd%\%ENV_FILE%
    echo Please ensure that the .env file exists and try again.
    pause
    exit /b 1
)

echo Found .env file

REM Run Docker Compose with the .env file
echo Running Docker Compose...
docker-compose -p adminbot --env-file "%ENV_FILE%" -f Docker/docker-compose.yml up --build

pause
exit /b 0
