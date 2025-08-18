# Inputs (add to variables.tf if not present):
variable "webapp_identity_object_id" {
  description = "Object ID of the Web App managed identity."
  type        = string
}

variable "terraform_deployer_object_id" {
  description = "Object ID of the SP that runs Terraform; gets App Configuration Data Owner (optional)."
  type        = string
  default     = null
}

# Role definitions on the App Configuration scope
data "azurerm_role_definition" "appcfg_data_reader" {
  name  = "App Configuration Data Reader"
  scope = azurerm_app_configuration.appcfg.id
}

data "azurerm_role_definition" "appcfg_data_owner" {
  name  = "App Configuration Data Owner"
  scope = azurerm_app_configuration.appcfg.id
}

# Runtime: Web App MI can read App Configuration
resource "azurerm_role_assignment" "webapp_appcfg_reader" {
  scope              = azurerm_app_configuration.appcfg.id
  role_definition_id = data.azurerm_role_definition.appcfg_data_reader.id
  principal_id       = var.webapp_identity_object_id

  depends_on = [azurerm_app_configuration.appcfg]
}

# Optional: Terraform deployer can manage data-plane (keys/features)
resource "azurerm_role_assignment" "tf_appcfg_owner" {
  count              = var.terraform_deployer_object_id == null ? 0 : 1
  scope              = azurerm_app_configuration.appcfg.id
  role_definition_id = data.azurerm_role_definition.appcfg_data_owner.id
  principal_id       = var.terraform_deployer_object_id

  depends_on = [azurerm_app_configuration.appcfg]
}
