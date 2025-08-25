# Start a persistent Azurite container for local dev/VS
$Name = "irs_azurite_dev"
$Key  = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVG=="

# If exists, just ensure it's running
$exists = (docker ps -a --format "{{.Names}}" | Select-String -SimpleMatch $Name)
if ($exists) {
  docker start $Name | Out-Null
} else {
  docker run -d --name $Name -p 10000:10000 `
    -e AZURITE_ACCOUNTS="devstoreaccount1:$Key" `
    -v "$PWD\.azurite:/data" `
    mcr.microsoft.com/azure-storage/azurite `
    azurite --blobHost 0.0.0.0 --loose --skipApiVersionCheck --location /data --debug /data/debug.log | Out-Null
}

# quick health check
$ok = $false
for ($i=0; $i -lt 20; $i++) {
  Start-Sleep -Seconds 1
  try {
    $r = Invoke-WebRequest -UseBasicParsing -Uri "http://localhost:10000/devstoreaccount1?comp=list" -Method Get
    if ($r.StatusCode -in 200,202) { $ok = $true; break }
  } catch { Start-Sleep -Milliseconds 250 }
}
if (-not $ok) { throw "Azurite on http://localhost:10000 is not responding." }
Write-Host "Azurite is up on http://localhost:10000"
