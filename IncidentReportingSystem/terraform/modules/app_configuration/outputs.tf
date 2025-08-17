output "endpoint" {
  value       = azurerm_app_configuration.this.endpoint
  description = "App Configuration endpoint"
}

output "id" {
  value       = azurerm_app_configuration.this.id
  description = "App Configuration resource ID"
}
