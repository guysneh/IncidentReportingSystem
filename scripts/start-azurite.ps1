# Start a persistent Azurite container for local dev/VS
$Name = "irs_azurite_dev"
# Well-known Azurite dev key (base64, exact string)
$Key  = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw=="

# If exists, just ensure it's running
$exists = (docker ps -a --format "{{.Names}}" | Select-String -SimpleMatch $Name)
if ($exists) {
  docker start $Name | Out-Null
} else {
  # Remove any stale container with the same name, just in case
  docker rm -f $Name 2>$null | Out-Null

  docker run -d --name $Name `
    -p 10000:10000 `
    -e AZURITE_ACCOUNTS="devstoreaccount1:$Key" `
    -v "$PWD\.azurite:/data" `
    mcr.microsoft.com/azure-storage/azurite `
    azurite --blobHost 0.0.0.0 --loose --skipApiVersionCheck --location /data --debug /data/debug.log `
    | Out-Null
}

# Quick health check: any HTTP response means the service is up (403/404 included)
$ok = $false
for ($i=0; $i -lt 20; $i++) {
  Start-Sleep -Milliseconds 400
  try {
    $r = Invoke-WebRequest -UseBasicParsing -Uri "http://127.0.0.1:10000/" -Method Get -TimeoutSec 2
    $ok = $true; break
  } catch {
    # If we got a response with a status code (e.g., 403/404), it's also considered "up"
    if ($_.Exception.Response -ne $null) { $ok = $true; break }
  }
}
if (-not $ok) {
  Write-Error "Azurite on http://127.0.0.1:10000 is not responding. Run: docker logs $Name"
  exit 1
}

Write-Host "Azurite is up on http://127.0.0.1:10000"

# Print a dev connection string you can paste into appsettings.Development.json
$cs = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=$Key;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;"
Write-Host "Local dev connection string:" -ForegroundColor Cyan
Write-Host $cs
