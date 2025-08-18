output "principal_id" {
  value = azurerm_linux_web_app.this.identity[0].principal_id
}

output "id" {
  value = azurerm_linux_web_app.this.id
}

output "default_hostname" {
  value = azurerm_linux_web_app.this.default_hostname
}

output "outbound_ip_addresses" {
  description = "Comma-separated list of outbound IP addresses of the Web App"
  value       = azurerm_linux_web_app.this.outbound_ip_addresses
}

output "possible_outbound_ip_addresses" {
  value = azurerm_linux_web_app.this.possible_outbound_ip_addresses
}