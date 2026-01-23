# Test Choreography with ClientA and ClientB (with Polly)
# Both clients subscribe to OrderPlaced events
# Make sure ClientA, ClientB, and SenderApp are running

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Choreography Pattern Testing Script" -ForegroundColor Cyan
Write-Host "Both ClientA and ClientB subscribe to OrderPlaced events" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "https://localhost:7294"  # Update if your SenderApp runs on a different port

Write-Host "Ensure the following are running:" -ForegroundColor Yellow
Write-Host "  1. RabbitMQ (Docker or local)" -ForegroundColor Yellow
Write-Host "  2. SenderApp" -ForegroundColor Yellow
Write-Host "  3. ClientA" -ForegroundColor Yellow
Write-Host "  4. ClientB" -ForegroundColor Yellow
Write-Host ""
Read-Host "Press Enter to continue..."

Write-Host ""
Write-Host "=========================================" -ForegroundColor Green
Write-Host "Test 1: SUCCESS Scenario" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
Write-Host "Publishing OrderPlaced event without exception type..."
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/order/process" -Method Post -SkipCertificateCheck
    Write-Host "Response:" -ForegroundColor Green
    $response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10
    Write-Host ""
    Write-Host "? Check BOTH ClientA and ClientB consoles:" -ForegroundColor Green
    Write-Host "  - ClientA: Should process the event normally" -ForegroundColor Green
    Write-Host "  - ClientB: Should show SUCCESS message" -ForegroundColor Green
} catch {
    Write-Host "Error: $_" -ForegroundColor Red
}

Write-Host ""
Read-Host "Press Enter for next test..."

Write-Host ""
Write-Host "=========================================" -ForegroundColor Yellow
Write-Host "Test 2: RETRY Scenario (ClientB only)" -ForegroundColor Yellow
Write-Host "=========================================" -ForegroundColor Yellow
Write-Host "Publishing event with exceptionType=retry..."
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/order/process?exceptionType=retry" -Method Post -SkipCertificateCheck
    Write-Host "Response:" -ForegroundColor Yellow
    $response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10
    Write-Host ""
    Write-Host "? Check consoles:" -ForegroundColor Yellow
    Write-Host "  - ClientA: Processes normally (ignores ExceptionType)" -ForegroundColor Green
    Write-Host "  - ClientB: Shows [POLLY RETRY] messages with 3 retry attempts" -ForegroundColor Yellow
    Write-Host "           Then fails after all retries exhausted" -ForegroundColor Yellow
} catch {
    Write-Host "Error: $_" -ForegroundColor Red
}

Write-Host ""
Read-Host "Press Enter for next test..."

Write-Host ""
Write-Host "=========================================" -ForegroundColor Red
Write-Host "Test 3: CIRCUIT BREAKER Scenario (ClientB only)" -ForegroundColor Red
Write-Host "=========================================" -ForegroundColor Red
Write-Host "Publishing multiple events to trigger circuit breaker in ClientB..."

for ($i = 1; $i -le 5; $i++) {
    Write-Host ""
    Write-Host "--- Publishing Event $i ---" -ForegroundColor Magenta
    try {
        $response = Invoke-WebRequest -Uri "$baseUrl/order/process?exceptionType=circuit-breaker" -Method Post -SkipCertificateCheck
        Write-Host "Response:" -ForegroundColor Cyan
        $response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10
    } catch {
        Write-Host "Error (expected): $_" -ForegroundColor DarkGray
    }
    
    if ($i -eq 3) {
        Write-Host ""
        Write-Host "? Circuit should OPEN in ClientB after this event!" -ForegroundColor Red
        Write-Host "  Watch ClientB console for [POLLY CIRCUIT BREAKER] Circuit OPENED!" -ForegroundColor Red
    }
    
    Start-Sleep -Seconds 1
}

Write-Host ""
Write-Host "? Check consoles:" -ForegroundColor Red
Write-Host "  - ClientA: Processes all 5 events normally" -ForegroundColor Green
Write-Host "  - ClientB: First 3 attempts fail normally" -ForegroundColor Yellow
Write-Host "           Circuit OPENS message appears" -ForegroundColor Red
Write-Host "           Subsequent requests rejected with BrokenCircuitException" -ForegroundColor Red

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Testing Complete!" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Architecture Pattern: CHOREOGRAPHY" -ForegroundColor Green
Write-Host "  - SenderApp PUBLISHES OrderPlaced events" -ForegroundColor White
Write-Host "  - ClientA subscribes (processes normally)" -ForegroundColor White
Write-Host "  - ClientB subscribes (uses Polly for resilience)" -ForegroundColor White
Write-Host "  - No direct coupling between sender and receivers" -ForegroundColor White
Write-Host ""
Write-Host "Circuit Breaker Recovery:" -ForegroundColor Yellow
Write-Host "  - Circuit will stay OPEN for 30 seconds" -ForegroundColor Yellow
Write-Host "  - Then transitions to HALF-OPEN to test recovery" -ForegroundColor Yellow
Write-Host "  - Wait 30 seconds and try test 3 again to see HALF-OPEN behavior" -ForegroundColor Yellow
Write-Host ""
