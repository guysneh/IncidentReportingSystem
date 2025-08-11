output "connection_string" {
  value       = azurerm_application_insights.this.connection_string
  description = "Application Insights connection string"
}

output "instrumentation_key" {
  value       = azurerm_application_insights.this.instrumentation_key
  description = "Legacy instrumentation key"
}
