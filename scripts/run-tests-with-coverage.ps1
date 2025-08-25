cd ..
cd .\IncidentReportingSystem\
# Step 1: Clean and Build
dotnet clean
dotnet build --no-incremental

# Step 2: Remove old coverage data
Remove-Item -Path "coverage.*.opencover.xml", "coveragereport" -Recurse -Force -ErrorAction SilentlyContinue

# Step 3: Set test project paths
$unitTestDll = ".\IncidentReportingSystem.Tests\bin\Debug\net8.0\IncidentReportingSystem.Tests.dll"
$integrationTestDll = ".\IncidentReportingSystem.IntegrationTests\bin\Debug\net8.0\IncidentReportingSystem.IntegrationTests.dll"

# Step 4: Start PostgreSQL test container (port 5444)
docker run --name irs_testdb -d -p 5444:5432 `
  -e POSTGRES_USER=testuser `
  -e POSTGRES_PASSWORD=testpassword `
  -e POSTGRES_DB=testdb `
  postgres:15-alpine

# === START: Azurite (Blob) for integration tests ===
$azuriteName = "irs_azurite_test"

# Clean any stale container
docker rm -f $azuriteName | Out-Null 2>$null

# Run full 'azurite' (not 'azurite-blob') and bind to 0.0.0.0
docker run --name $azuriteName -d -p 10000:10000 `
  -v azurite_data:/data `
  mcr.microsoft.com/azure-storage/azurite `
  azurite --blobHost 0.0.0.0 --location /data --debug /data/debug.log | Out-Null

Write-Host "Waiting for Azurite (TCP 10000) up to 120s..." -ForegroundColor Cyan
$ok = $false
$deadline = (Get-Date).AddSeconds(120)

while ((Get-Date) -lt $deadline) {
  # Ensure container is running
  $state = (docker inspect -f "{{.State.Status}}" $azuriteName 2>$null)
  if ($state -ne "running") { Start-Sleep -Seconds 1; continue }

  # TCP check only (more reliable than HTTP probe for Azurite)
  $tcp = Test-NetConnection -ComputerName 127.0.0.1 -Port 10000 -InformationLevel Quiet
  if ($tcp) { $ok = $true; break }

  Start-Sleep -Seconds 1
}

if (-not $ok) {
  Write-Host "Azurite did not become healthy in time. Dumping recent logs:" -ForegroundColor Yellow
  docker logs --tail 200 $azuriteName 2>&1 | Write-Host
  throw "Azurite did not become healthy in time."
}
# === END: Azurite ===


# Step 5: Wait for DB to be ready
Write-Host "Waiting for PostgreSQL to be ready..."
Start-Sleep -Seconds 5

# Step 6: Set connection string
$env:TEST_DB_CONNECTION = "Host=localhost;Port=5444;Database=testdb;Username=testuser;Password=testpassword"
# Step 6.1: Ensure Azurite is available on localhost:10000 (start if needed)
$azuriteStarted = $false
try {
  $tcp = Test-NetConnection -ComputerName localhost -Port 10000 -WarningAction SilentlyContinue
  if (-not $tcp.TcpTestSucceeded) {
    Write-Host "Starting Azurite for tests..."
    docker run --name irs_azurite -d -p 10000:10000 `
      -e AZURITE_ACCOUNTS="devstoreaccount1:Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVG==" `
      mcr.microsoft.com/azure-storage/azurite `
      azurite --blobHost 0.0.0.0 --loose --skipApiVersionCheck --location /data --debug /data/debug.log | Out-Null
    $azuriteStarted = $true

    Write-Host "Waiting for Azurite to listen on 10000..."
    $waitOk = $false
    for ($i=0; $i -lt 30; $i++) {
      Start-Sleep -Seconds 1
      $t = Test-NetConnection -ComputerName localhost -Port 10000 -WarningAction SilentlyContinue
      if ($t.TcpTestSucceeded) { $waitOk = $true; break }
    }
    if (-not $waitOk) { throw "Azurite did not become ready on port 10000." }
  } else {
    Write-Host "Azurite already listening on 10000. Reusing."
  }
} catch {
  throw $_
}

# -------- Unit --------
coverlet $unitTestDll `
  --target "dotnet" `
  --targetargs "test IncidentReportingSystem.Tests/IncidentReportingSystem.Tests.csproj --no-build --verbosity normal" `
  --format opencover `
  --output "coverage.unit.opencover.xml" `
  --threshold 80 --threshold-type line --threshold-stat total `
  --exclude "[IncidentReportingSystem.API]*Program*" `
  --exclude "[IncidentReportingSystem.API]*ObservabilityExtensions*" `
  --exclude-by-file "**/ObservabilityExtensions*.cs" `
  --exclude-by-file "**/Extensions/ServiceCollection*.cs" `
  --exclude-by-file "**/Extensions/DependencyInjection/*.cs" `
  --exclude-by-file "**/Migrations/*.cs" `
  --exclude-by-file "**/*Migrations/*.cs" `
  --exclude-by-file "**/Dtos/**" `
  --exclude-by-file "**/*Dto.cs" `
  --exclude-by-file "**/Program.cs" `
  --exclude-by-file "**/ConfigureSwagger*.cs" `
  --exclude-by-file "**/GlobalUsings.cs" `
  --exclude-by-file "**/Generated/**" `
  --exclude-by-file "**/*.g.cs" `
  --exclude-by-attribute "GeneratedCodeAttribute" `
  --exclude-by-attribute "CompilerGeneratedAttribute" `
  --exclude-by-attribute "ExcludeFromCodeCoverageAttribute" `
  --exclude-by-file "**/Persistence/DesignTimeDbContextFactory.cs" 

# -------- Integration --------
# === START: Azurite env for integration tests ===
$env:Attachments__Storage = "Azurite"
$env:Attachments__Container = "attachments"
$env:Storage__Blob__Endpoint = "http://127.0.0.1:10000/devstoreaccount1"
$env:Storage__Blob__AccountName = "devstoreaccount1"
$env:Storage__Blob__AccountKey = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVG=="

# === END: Azurite env ===

coverlet $integrationTestDll `
  --target "dotnet" `
  --targetargs "test IncidentReportingSystem.IntegrationTests/IncidentReportingSystem.IntegrationTests.csproj --no-build --verbosity normal" `
  --format opencover `
  --output "coverage.integration.opencover.xml" `
  --threshold 80 --threshold-type line --threshold-stat total `
  --exclude "[IncidentReportingSystem.API]*Program*" `
  --exclude "[IncidentReportingSystem.API]*ObservabilityExtensions*" `
  --exclude-by-file "**/ObservabilityExtensions*.cs" `
  --exclude-by-file "**/Extensions/ServiceCollection*.cs" `
  --exclude-by-file "**/Extensions/DependencyInjection/*.cs" `
  --exclude-by-file "**/Migrations/*.cs" `
  --exclude-by-file "**/*Migrations/*.cs" `
  --exclude-by-file "**/Dtos/**" `
  --exclude-by-file "**/*Dto.cs" `
  --exclude-by-file "**/Program.cs" `
  --exclude-by-file "**/ConfigureSwagger*.cs" `
  --exclude-by-file "**/GlobalUsings.cs" `
  --exclude-by-file "**/Generated/**" `
  --exclude-by-file "**/*.g.cs" `
  --exclude-by-attribute "GeneratedCodeAttribute" `
  --exclude-by-attribute "CompilerGeneratedAttribute" `
  --exclude-by-attribute "ExcludeFromCodeCoverageAttribute" `
  --exclude-by-file "**/Persistence/DesignTimeDbContextFactory.cs" 

# Step 9: Merge both coverage files
reportgenerator `
  -reports:"coverage.unit.opencover.xml;coverage.integration.opencover.xml" `
  -targetdir:"coveragereport" `
  -reporttypes:"HtmlInline_AzurePipelines;TextSummary"

# Step 10: Open report
Start-Process "coveragereport\index.html"

# Step 11: Stop and remove container
docker stop irs_testdb | Out-Null
docker rm irs_testdb | Out-Null

# === START: Cleanup Azurite ===
docker stop $azuriteName | Out-Null 2>$null
docker rm $azuriteName | Out-Null 2>$null
# === END: Cleanup Azurite ===
if ($azuriteStarted) {
  docker stop irs_azurite | Out-Null
  docker rm irs_azurite   | Out-Null
}
