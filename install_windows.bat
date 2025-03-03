@echo off
echo Starting Discord Bot Installation...

REM Check if Docker is installed
docker --version > nul 2>&1
if %errorlevel% neq 0 (
    echo Error: Docker is not installed!
    echo Please install Docker Desktop from https://www.docker.com/products/docker-desktop
    pause
    exit /b 1
)

REM Check if .env file exists
if not exist .env (
    echo Creating .env file from template...
    copy .env.example .env
    echo Please edit the .env file with your configuration before continuing.
    notepad .env
)

REM Build and start containers
echo Building Docker containers...
docker-compose build

echo Starting services...
docker-compose up -d

echo.
echo Installation complete! 
echo.
echo The bot should now be running. Check Docker Desktop for status.
echo Logs are available in the ./logs directory.
echo.
echo Press any key to view logs...
pause > nul

REM Show logs
docker-compose logs -f