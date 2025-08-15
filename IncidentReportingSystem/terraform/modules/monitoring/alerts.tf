# 1) 5xx count with min volume over 10 minutes (reduces noise)
resource "azurerm_monitor_scheduled_query_rules_alert" "ai_5xx" {
  name                = "incident-5xx-alert"
  location            = azurerm_log_analytics_workspace.law.location
  resource_group_name = var.resource_group_name
  data_source_id      = azurerm_log_analytics_workspace.law.id

  description = "Alert when HTTP 5xx observed with minimum traffic over the last 10 minutes."
  enabled     = true
  severity    = 2
  time_window = 10
  frequency   = 5

  query = <<-KQL
    let Window = 10m;
    let MinTotal = 100;
    let Min5xx   = 10;
    // Use 'requests' or 'AppRequests' according to your workspace schema:
    requests
    | where timestamp > ago(Window)
    | where client_Type !in ("Synthetic", "Availability")
    | extend code = toint(resultCode)
    | summarize total = count(), server_errors = countif(code between (500 .. 599))
    | project AggregatedValue = iff(total >= MinTotal and server_errors >= Min5xx, 1, 0)
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
    let Window = 5m;
    requests
    | where timestamp > ago(Window)
    | where client_Type !in ("Synthetic", "Availability")
    | where url endswith "/health"
    | extend code = toint(resultCode)
    | summarize non_200 = countif(code != 200)
    | project AggregatedValue = iff(non_200 > 0, 1, 0)
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

resource "azurerm_monitor_scheduled_query_rules_alert" "error_rate" {
  name                = "incident-error-rate-alert"
  location            = var.location
  resource_group_name = var.resource_group_name
  description         = "Alert when 5xx error rate exceeds 2% over 10 minutes with min volume (>=100 requests)"
  severity            = 2
  enabled             = true
  frequency           = 5
  time_window         = 10

  # Use the workspace here as well (NOT the AI resource id)
  data_source_id = azurerm_log_analytics_workspace.law.id

  query = <<-KQL
    let Window = 10m;
    let MinTotal = 100;
    let Threshold = 0.02;
    requests
    | where timestamp > ago(Window)
    | where client_Type !in ("Synthetic", "Availability")
    | extend code = toint(resultCode)
    | summarize total = count(), server_errors = countif(code between (500 .. 599))
    | extend error_rate = iif(total == 0, 0.0, todouble(server_errors) / todouble(total))
    | project AggregatedValue = iff(total >= MinTotal and error_rate > Threshold, 1, 0)
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

resource "azurerm_monitor_scheduled_query_rules_alert" "p95_latency" {
  name                = "incident-p95-latency-alert"
  location            = azurerm_log_analytics_workspace.law.location
  resource_group_name = var.resource_group_name
  data_source_id      = azurerm_log_analytics_workspace.law.id
  description         = "Alert when request duration P95 exceeds 2 seconds over the last 10 minutes."
  severity            = 2
  enabled             = true
  time_window         = 10 # דקות
  frequency           = 5  # דקות

  query = <<-KQL
    let Window = 10m;

    requests
    | where timestamp > ago(Window)
    | where client_Type !in ("Synthetic", "Availability")
    | summarize p95 = percentile(duration, 95)
    | project AggregatedValue = iff(p95 > 2s, 1, 0)
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
