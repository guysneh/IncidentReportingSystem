output "account_name" {
  value = azurerm_storage_account.this.name
}

output "account_id" {
  value = azurerm_storage_account.this.id
}

output "blob_endpoint" {
  value = azurerm_storage_account.this.primary_blob_endpoint
}

output "container_name" {
  value = azurerm_storage_container.attachments.name
}
