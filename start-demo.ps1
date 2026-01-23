# Quick Start Script for the Demo
# Run this from the solution root directory

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "NServiceBus Choreography Pattern Demo" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

# Check if Docker is running
Write-Host "Checking Docker..." -ForegroundColor Yellow
try {
    docker ps | Out-Null
    Write-Host "? Docker is running" -ForegroundColor Green
} catch {
    Write-Host "? Docker is not running. Please start Docker Desktop." -ForegroundColor Red
    exit 1
}

# Start RabbitMQ
Write-Host ""
Write-Host "Starting RabbitMQ..." -ForegroundColor Yellow
docker-compose up -d

Start-Sleep -Seconds 3

Write-Host "? RabbitMQ is starting" -ForegroundColor Green
Write-Host "  Management UI: http://localhost:15672 (guest/guest)" -ForegroundColor Gray

# Build the solution
Write-Host ""
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build
if ($LASTEXITCODE -ne 0) {
    Write-Host "? Build failed" -ForegroundColor Red
    exit 1
}
Write-Host "? Build successful" -ForegroundColor Green

# Instructions
Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Start ClientA in a new terminal:" -ForegroundColor White
Write-Host "   cd ClientA" -ForegroundColor Gray
Write-Host "   dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Start SenderApp in another new terminal:" -ForegroundColor White
Write-Host "   cd SenderApp" -ForegroundColor Gray
Write-Host "   dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Test the API:" -ForegroundColor White
Write-Host "   - Open https://localhost:5001/swagger" -ForegroundColor Gray
Write-Host "   - Execute GET /Order" -ForegroundColor Gray
Write-Host "   - Watch ClientA console for the message!" -ForegroundColor Gray
Write-Host ""
Write-Host "Or use this command to test:" -ForegroundColor White
Write-Host "   Invoke-WebRequest -Uri https://localhost:5001/Order -Method GET -SkipCertificateCheck" -ForegroundColor Gray
Write-Host ""
Write-Host "Press any key to continue..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
