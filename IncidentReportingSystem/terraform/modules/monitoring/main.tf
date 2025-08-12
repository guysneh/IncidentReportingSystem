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
  description         = "Alert when error rate exceeds 5% in 5 minutes"
  severity            = 2
  enabled             = true

  # NOTE: for this provider version, use minutes as integers:
  frequency   = 5        # evaluate every 5 minutes
  time_window = 5        # look back 5 minutes

  # NOTE: in this schema we pass a single data source, not 'scopes'
  data_source_id = azurerm_application_insights.appi.id

  query = <<-KQL
    let Window = 5m;
    let Err = toscalar(requests
        | where timestamp > ago(Window)
        | summarize err_count = countif(success == false));
    let All = toscalar(requests
        | where timestamp > ago(Window)
        | summarize total = count());
    print error_rate = todouble(Err) / todouble(All)
    | where error_rate > 0.05
  KQL

  # For this schema, 'trigger' is mandatory; we trigger on "any result row"
  trigger {
    operator  = "GreaterThan"
    threshold = 0
  }

  # For this schema, the 'action' block expects 'action_group' as a LIST
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
