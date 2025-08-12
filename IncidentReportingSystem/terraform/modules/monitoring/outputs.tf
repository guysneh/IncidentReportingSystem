output "connection_string" {
  value = azurerm_application_insights.appi.connection_string
}

output "instrumentation_key" {
  value = azurerm_application_insights.appi.instrumentation_key
}

output "action_group_id" {
  value = azurerm_monitor_action_group.this.id
}