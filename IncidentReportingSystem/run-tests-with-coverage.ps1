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

# Step 5: Wait for DB to be ready
Write-Host "Waiting for PostgreSQL to be ready..."
Start-Sleep -Seconds 5

# Step 6: Set connection string
$env:TEST_DB_CONNECTION = "Host=localhost;Port=5444;Database=testdb;Username=testuser;Password=testpassword"

# Step 7: Run unit test coverage
coverlet $unitTestDll `
  --target "dotnet" `
  --targetargs "test IncidentReportingSystem.Tests/IncidentReportingSystem.Tests.csproj --no-build --verbosity normal" `
  --format opencover `
  --output "coverage.unit.opencover.xml"

# Step 8: Run integration test coverage
coverlet $integrationTestDll `
  --target "dotnet" `
  --targetargs "test IncidentReportingSystem.IntegrationTests/IncidentReportingSystem.IntegrationTests.csproj --no-build --verbosity normal" `
  --format opencover `
  --output "coverage.integration.opencover.xml"

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
