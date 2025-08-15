# 1) 5xx count with min volume over 10 minutes (reduces noise)
resource "azurerm_monitor_scheduled_query_rules_alert" "ai_5xx" {
  name                = "incident-5xx-alert"
  location            = azurerm_log_analytics_workspace.law.location
  resource_group_name = var.resource_group_name

  # Keep using the workspace as data source (tables include 'requests' in workspace-based AI)
  data_source_id = azurerm_log_analytics_workspace.law.id

  description = "Alert when HTTP 5xx observed with minimum traffic over the last 10 minutes."
  enabled     = true
  severity    = 2

  time_window = 10  # minutes
  frequency   = 5   # minutes

  query = <<-KQL
    let Window = 10m;
    requests
    | where timestamp > ago(Window)
    | extend code = toint(resultCode)
    | summarize total = count(), server_errors = countif(code between (500 .. 599))
    | project AggregatedValue = iff(total >= 100 and server_errors >= 10, 1, 0)
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
