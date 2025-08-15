##############################################
# Log Analytics Workspace
##############################################
resource "azurerm_log_analytics_workspace" "law" {
  name                = "${var.resource_group_name}-law"
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = "PerGB2018"
  retention_in_days   = var.log_analytics_retention_days
  tags                = var.tags
}

##############################################
# Application Insights (Workspace-based)
##############################################
resource "azurerm_application_insights" "appi" {
  name                = "${var.resource_group_name}-appi"
  location            = var.location
  resource_group_name = var.resource_group_name
  application_type    = "web"
  workspace_id        = azurerm_log_analytics_workspace.law.id
  tags                = var.tags
}

##############################################
# Action Group for Alerts
##############################################
resource "azurerm_monitor_action_group" "this" {
  name                = var.action_group_name
  resource_group_name = var.resource_group_name
  short_name          = "incidentAG"

  email_receiver {
    name                    = "PrimaryEmail"
    email_address           = var.action_group_email
    use_common_alert_schema = true
  }

  tags = var.tags
}

