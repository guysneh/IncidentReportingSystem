# run-tests-with-coverage.ps1

# נקה קודם כל
dotnet clean

# נבנה את כל הסולושן מחדש
dotnet build --no-incremental

# ננקה דוחות קודמים
Remove-Item -Path "coverage.opencover.xml", "coveragereport" -Recurse -Force -ErrorAction SilentlyContinue

# הגדר את הנתיב לקובץ הטסטים
$testDllPath = ".\IncidentReportingSystem.Tests\bin\Debug\net8.0\IncidentReportingSystem.Tests.dll"

# הרץ טסטים ואסוף קוברג'
coverlet $testDllPath `
  --target "dotnet" `
  --targetargs "test --no-build --verbosity normal" `
  --format opencover

# הפק דו"ח HTML
reportgenerator `
  -reports:"coverage.opencover.xml" `
  -targetdir:"coveragereport" `
  -reporttypes:"HtmlInline_AzurePipelines,TextSummary"

# פתח את הדוח בדפדפן
Start-Process "coveragereport\index.html"
