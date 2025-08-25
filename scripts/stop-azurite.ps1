# Stop and keep the container/images for next time
$Name = "irs_azurite_dev"
docker stop $Name 2>$null | Out-Null
Write-Host "Azurite stopped."
