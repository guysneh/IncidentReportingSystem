output "gha_app_client_id" {
  value = azuread_application.gha.client_id
}

output "gha_sp_object_id" {
  value = azuread_service_principal.gha.object_id
}

output "app_config_endpoint" {
  value       = module.app_configuration.endpoint
  description = "Azure App Configuration endpoint"
}