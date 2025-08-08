# --- General Infrastructure Settings ---

variable "resource_group_name" {
  type        = string
  description = "The name of the resource group"
  default     = "incident-rg"
}

variable "location" {
  type        = string
  description = "Azure location for all resources"
  default     = "westeurope"
}

variable "default_tags" {
  type        = map(string)
  description = "Tags to be applied to all resources"
  default = {
    Project     = "IncidentReportingSystem"
    Environment = "Production"
    Owner       = "Guy Sne"
    CostCenter  = "HomeLab"
  }
}

# --- Budget & Cost Management ---

variable "budget_amount" {
  type        = number
  description = "Monthly budget limit in USD"
  default     = 20
}

variable "budget_threshold_warn" {
  type        = number
  description = "First alert threshold (%)"
  default     = 80
}

variable "budget_threshold_crit" {
  type        = number
  description = "Critical alert threshold (%)"
  default     = 100
}

variable "notification_emails" {
  type        = list(string)
  description = "List of email addresses to notify on budget alerts"
  default     = ["guysneh@gmail.com"] # שנה לפי הצורך
}
