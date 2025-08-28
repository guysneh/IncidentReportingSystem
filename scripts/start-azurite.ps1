param(
    [int]$BlobPort = 11000,
    [int]$QueuePort = 11001,
    [int]$TablePort = 11002,
    [string]$ContainerName = "azurite"
)

Write-Host "Starting Azurite on ports:"
Write-Host "  Blob   = $BlobPort"
Write-Host "  Queue  = $QueuePort"
Write-Host "  Table  = $TablePort"

# Stop and remove any old container with the same name
if (docker ps -a --format "{{.Names}}" | Select-String -Pattern "^$ContainerName$") {
    Write-Host "Removing old container '$ContainerName'..."
    docker rm -f $ContainerName | Out-Null
}

# Run Azurite
docker run -d `
    --name $ContainerName `
    -p ${BlobPort}:10000 `
    -p ${QueuePort}:10001 `
    -p ${TablePort}:10002 `
    mcr.microsoft.com/azure-storage/azurite | Out-Null

# Health check (simple loop: try to connect to Blob endpoint)
$maxRetries = 10
$ok = $false
for ($i = 0; $i -lt $maxRetries; $i++) {
    try {
        $resp = Invoke-WebRequest -Uri "http://127.0.0.1:$BlobPort/devstoreaccount1?comp=list" -UseBasicParsing -TimeoutSec 2
        if ($resp.StatusCode -eq 200 -or $resp.StatusCode -eq 400) {
            $ok = $true
            break
        }
    } catch {
        Start-Sleep -Seconds 2
    }
}

if (-not $ok) {
    throw "Azurite on http://127.0.0.1:$BlobPort did not become healthy."
}

Write-Host "Azurite started successfully on http://127.0.0.1:$BlobPort"
