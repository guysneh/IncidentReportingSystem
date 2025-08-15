##############################################
# Log Analytics Workspace
##############################################
resource "azurerm_log_analytics_workspace" "law" {
  name                = "${var.resource_group_name}-law"
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = "PerGB2018"
  retention_in_days   = 30
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

##############################################
# Log Alert Rule: Error Rate > 5% in 5 minutes
##############################################
resource "azurerm_monitor_scheduled_query_rules_alert" "error_rate" {
  name                = "incident-error-rate-alert"
  location            = var.location
  resource_group_name = var.resource_group_name
  description         = "Alert when 5xx error rate exceeds 2% over 10 minutes with min volume (>=100 requests)"
  severity            = 2
  enabled             = true

  # Evaluate every 5 minutes over a 10-minute window
  frequency   = 5   # minutes
  time_window = 10  # minutes

  # Use the Application Insights component as the data source (workspace-based AI is fine)
  data_source_id = azurerm_application_insights.appi.id

  # Fire only when both min volume AND error rate threshold are exceeded.
  query = <<-KQL
    let Window = 10m;
    requests
    | where timestamp > ago(Window)
    | extend code = toint(resultCode)
    | summarize total = count(), server_errors = countif(code between (500 .. 599))
    | extend error_rate = todouble(server_errors) / todouble(total)
    | project AggregatedValue = iff(total >= 100 and error_rate > 0.02, 1, 0)
  KQL

  trigger {
    operator  = "GreaterThan"
    threshold = 0
  }

  action {
    action_group = [azurerm_monitor_action_group.this.id]
  }

  tags = var.tags
}


##############################################
# Metric Alert Rule: P95 Duration > 2s
##############################################
resource "azurerm_monitor_metric_alert" "p95_duration" {
  name                = "incident-p95-duration-alert"
  resource_group_name = var.resource_group_name
  scopes              = [azurerm_application_insights.appi.id]
  description         = "Alert when request duration exceeds 2 seconds (avg)"
  severity            = 2
  frequency           = "PT5M"
  window_size         = "PT5M"
  enabled             = true

  criteria {
    metric_namespace = "microsoft.insights/components"
    metric_name      = "requests/duration"
    aggregation      = "Average"
    operator         = "GreaterThan"
    threshold        = 2000
  }

  action {
    action_group_id = azurerm_monitor_action_group.this.id
  }

  tags = var.tags
}
