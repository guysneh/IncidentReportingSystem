# Step 1: Clean and Build
dotnet clean
dotnet build --no-incremental

# Step 2: Remove old coverage data
Remove-Item -Path "coverage.opencover.xml", "coveragereport" -Recurse -Force -ErrorAction SilentlyContinue

# Step 3: Set test project paths
$unitTestDll = ".\IncidentReportingSystem.Tests\bin\Debug\net8.0\IncidentReportingSystem.Tests.dll"
$integrationTestDll = ".\IncidentReportingSystem.IntegrationTests\bin\Debug\net8.0\IncidentReportingSystem.IntegrationTests.dll"

# Step 4: Run unit test coverage
coverlet $unitTestDll `
  --target "dotnet" `
  --targetargs "test IncidentReportingSystem.Tests/IncidentReportingSystem.Tests.csproj --no-build --verbosity normal" `
  --format opencover `
  --output "coverage.unit.opencover.xml"

# Step 5: Run integration test coverage
coverlet $integrationTestDll `
  --target "dotnet" `
  --targetargs "test IncidentReportingSystem.IntegrationTests/IncidentReportingSystem.IntegrationTests.csproj --no-build --verbosity normal" `
  --format opencover `
  --output "coverage.integration.opencover.xml"

# Step 6: Merge both coverage files
reportgenerator `
  -reports:"coverage.unit.opencover.xml;coverage.integration.opencover.xml" `
  -targetdir:"coveragereport" `
  -reporttypes:"HtmlInline_AzurePipelines;TextSummary"

# Step 7: Open report
Start-Process "coveragereport\index.html"
