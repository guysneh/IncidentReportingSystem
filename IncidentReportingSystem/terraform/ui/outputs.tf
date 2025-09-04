output "ui_default_hostname" {
  value       = azurerm_linux_web_app.ui.default_hostname
  description = "Default hostname of the UI app (e.g., <name>.azurewebsites.net)"
}

output "ui_app_id" {
  value       = azurerm_linux_web_app.ui.id
  description = "Resource ID of the UI app"
}
