# Test Script - Sends a test order to the API
# Make sure SenderApp and ClientA are running before using this

param(
    [string]$BaseUrl = "https://localhost:5001",
    [int]$Count = 1
)

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "Testing Order API" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

for ($i = 1; $i -le $Count; $i++) {
    Write-Host "Sending order $i of $Count..." -ForegroundColor Yellow
    
    try {
        $response = Invoke-WebRequest -Uri "$BaseUrl/Order" -Method GET -SkipCertificateCheck
        $content = $response.Content | ConvertFrom-Json
        
        Write-Host "? Order placed successfully!" -ForegroundColor Green
        Write-Host "  Order ID: $($content.orderId)" -ForegroundColor Gray
        Write-Host "  Status: $($content.status)" -ForegroundColor Gray
        Write-Host ""
        
        if ($i -lt $Count) {
            Start-Sleep -Seconds 1
        }
    } catch {
        Write-Host "? Failed to place order: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
    }
}

Write-Host "Check the ClientA console to see the processed orders!" -ForegroundColor Cyan
