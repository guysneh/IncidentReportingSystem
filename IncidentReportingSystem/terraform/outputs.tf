output "gha_app_client_id" {
  value = azuread_application.gha.client_id
}

output "gha_sp_object_id" {
  value = azuread_service_principal.gha.object_id
}
