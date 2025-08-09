# --- General Infrastructure Settings ---

variable "resource_group_name" {
  type        = string
  description = "The name of the resource group"
  default     = "incident-rg"
}

variable "location" {
  type        = string
  description = "Azure location for all resources"
  default     = "northeurope"
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
  default     = ["guysneh@gmail.com"]
}

# --- PostgreSQL Access Credentials ---

variable "db_admin_username" {
  type        = string
  description = "Admin username for PostgreSQL Flexible Server"
}

variable "db_admin_password" {
  type        = string
  description = "Admin password for PostgreSQL Flexible Server"
  sensitive   = true
}

variable "postgres_connection_string" {
  description = "Full PostgreSQL connection string to store in Key Vault"
  type        = string
  sensitive   = true
}

variable "app_service_name" {
  description = "Web App name"
  type        = string
  default     = "incident-api"
}

variable "app_service_plan_sku_name" {
  description = "SKU name for the Service Plan (e.g., B1, P1v3)"
  type        = string
  default     = "B1"
}

variable "jwt_issuer" { type = string }
variable "jwt_audience" { type = string }
variable "jwt_expiry_minutes" {
  type    = number
  default = 60
}
variable "jwt_secret_length" {
  type    = number
  default = 64
}
variable "always_on" {
  type    = bool
  default = true
}