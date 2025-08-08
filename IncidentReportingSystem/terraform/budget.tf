data "azurerm_client_config" "current" {}

resource "azurerm_consumption_budget_subscription" "incident_budget" {
  name            = "${var.resource_group_name}-budget"
  subscription_id = "/subscriptions/${data.azurerm_client_config.current.subscription_id}"
  amount          = var.budget_amount
  time_grain      = "Monthly"

  time_period {
    start_date = "2025-08-01T00:00:00Z"
  }

  notification {
    enabled        = true
    operator       = "GreaterThan"
    threshold      = var.budget_threshold_warn
    threshold_type = "Actual"
    contact_emails = var.notification_emails
  }

  notification {
    enabled        = true
    operator       = "GreaterThan"
    threshold      = var.budget_threshold_crit
    threshold_type = "Actual"
    contact_emails = var.notification_emails
  }
}
