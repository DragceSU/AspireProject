param(
    [int]$PaymentInstances = 1,
    [int]$InvoiceInstances = 1,
    [string]$RabbitHost = "localhost",
    [switch]$Help
)

if ($Help) {
    Write-Host "Usage: .\run-services.ps1 [-PaymentInstances <int>] [-InvoiceInstances <int>] [-RabbitHost <host>] [-Help]" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Gray
    Write-Host "  -PaymentInstances <int>   Number of PaymentMicroservice windows to launch (default: 1)"
    Write-Host "  -InvoiceInstances <int>   Number of InvoiceMicroservice windows to launch (default: 1)" -ForegroundColor Gray
    Write-Host "  -RabbitHost <host>        RabbitMQ host (default: localhost)" -ForegroundColor Gray
    Write-Host "  -Help                     Show this help and exit" -ForegroundColor Gray
    return
}

Write-Host "Building solution..." -ForegroundColor Cyan
Push-Location "C:\Git\AspireProject\AppHost"
dotnet build AppHost.slnx
Pop-Location

Write-Host "Starting $InvoiceInstances InvoiceMicroservice instance(s) in new cmd window(s)..." -ForegroundColor Cyan
1..$InvoiceInstances | ForEach-Object {
    Start-Process cmd.exe -ArgumentList @(
        "/k",
        "cd /d C:\Git\AspireProject\AppHost\InvoiceMicroservice && set RABBIT_HOST=$RabbitHost && dotnet run"
    )
}

Write-Host "Starting $PaymentInstances PaymentMicroservice instance(s) in new windows..." -ForegroundColor Cyan
1..$PaymentInstances | ForEach-Object {
    Start-Process cmd.exe -ArgumentList @(
        "/k",
        "cd /d C:\Git\AspireProject\AppHost\PaymentMicroservice && set RABBIT_HOST=$RabbitHost && dotnet run"
    )
}

Write-Host "Jobs started. Use Get-Job to monitor InvoiceMicroservice output here." -ForegroundColor Green
