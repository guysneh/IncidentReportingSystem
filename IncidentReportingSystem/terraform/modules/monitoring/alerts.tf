# 1) 5xx in last 5 minutes
resource "azurerm_monitor_scheduled_query_rules_alert" "ai_5xx" {
  name                = "incident-5xx-alert"
  location            = azurerm_log_analytics_workspace.law.location
  resource_group_name = var.resource_group_name
  data_source_id      = azurerm_log_analytics_workspace.law.id

  description = "Alert when any HTTP 5xx responses are observed in the last 5 minutes."
  enabled     = true
  severity    = 2

  time_window = 5   # look back 5 minutes
  frequency   = 5   # evaluate every 5 minutes

  query = <<-KQL
requests
| where timestamp > ago(5m)
| where toint(resultCode) >= 500
| summarize Count = count()
KQL

  trigger {
    operator  = "GreaterThan"
    threshold = 0
  }

  action {
    action_group = [azurerm_monitor_action_group.this.id]
  }
}

# 2) /health non-200 in last 5 minutes
resource "azurerm_monitor_scheduled_query_rules_alert" "ai_health" {
  name                = "incident-health-alert"
  location            = azurerm_log_analytics_workspace.law.location
  resource_group_name = var.resource_group_name
  data_source_id      = azurerm_log_analytics_workspace.law.id

  description = "Alert when /health returns non-200 in the last 5 minutes."
  enabled     = true
  severity    = 2

  time_window = 5
  frequency   = 5

  query = <<-KQL
requests
| where timestamp > ago(5m)
| where url endswith "/health"
| where toint(resultCode) != 200
| summarize Count = count()
KQL

  trigger {
    operator  = "GreaterThan"
    threshold = 0
  }

  action {
    action_group = [azurerm_monitor_action_group.this.id]
  }
}
