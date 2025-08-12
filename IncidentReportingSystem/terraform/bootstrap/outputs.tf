output "resource_group_name" {
  value = azurerm_resource_group.tf.name
}

output "storage_account_name" {
  value = azurerm_storage_account.tf.name
}

output "container_name" {
  value = azurerm_storage_container.tf.name
}
