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
  default     = 100
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

variable "always_on" {
  type    = bool
  default = true
}

variable "log_analytics_retention_days" {
  type    = number
  default = 30
}

variable "name_prefix" {
  type = string
}

variable "tags" {
  type    = map(string)
  default = {}
}

variable "ai_retention_days" {
  type        = number
  description = "Application Insights retention in days"
  default     = 30
}

variable "jwt_expiry_minutes" {
  type        = number
  default     = 6000
  description = "JWT token expiry in minutes"
  validation {
    condition     = var.jwt_expiry_minutes >= 1 && var.jwt_expiry_minutes <= 1440
    error_message = "jwt_expiry_minutes must be between 1 and 1440."
  }
}

variable "action_group_email" {
  description = "Primary email address to receive Azure Monitor alerts"
  type        = string
  sensitive   = true
}

variable "postgres_database" {
  type        = string
  default     = "incidentdb"
  description = "Logical database name in the Postgres server"
}

variable "postgres_port" {
  type        = number
  description = "Port for PostgreSQL connections (Azure Flexible uses 5432 and it's not configurable)."
  default     = 5432
}

variable "ci_role_assignment_name" {
  type        = string
  description = "Existing RBAC assignment GUID for CI on Key Vault"
}

variable "demo_enable_config_probe" {
  type        = bool
  description = "Expose config-demo endpoint in non-Dev (protected by Admin policy)"
  default     = false
}

variable "demo_probe_auth_mode" {
  type        = string
  description = "Runtime auth mode for the probe endpoint: Admin or Anonymous"
  default     = "Admin"
  validation {
    condition     = contains(["Admin", "Anonymous"], var.demo_probe_auth_mode)
    error_message = "demo_probe_auth_mode must be 'Admin' or 'Anonymous'."
  }
}

variable "app_name" {
  type        = string
  description = "Human-friendly application name"
  default     = "Incident API"
}

variable "api_basepath" {
  type        = string
  description = "Base path prefix for the API"
  default     = "/api"
}

variable "api_version" {
  type        = string
  description = "Displayed API version (keep aligned with code default)"
  default     = "v1"
}

variable "subscription_id" {
  type        = string
  description = "Azure subscription ID to deploy into"
}

variable "tenant_id" {
  type        = string
  description = "Azure AD tenant ID"
}

variable "webapp_name" {
  description = "Name of the existing Linux Web App."
  type        = string
  default     = "incident-api"
}

variable "webapp_resource_group_name" {
  description = "Resource group of the Web App."
  type        = string
  default     = "incident-rg"
}

# Client ID of the SP that runs Terraform (from azure/login OIDC)
variable "terraform_deployer_client_id" {
  description = "Application (client) ID of the Service Principal running Terraform."
  type        = string
  default     = null
}

variable "telemetry_sample_ratio" {
  description = "Telemetry sample ratio of logs"
  type        = string
  default     = "0.10"
}

variable "web_app_name" {
  description = "Linux Web App name (API)"
  type        = string
  default     = "incident-api"
}

variable "app_config_label" {
  description = "Label (prod/dev/blue/green)"
  type        = string
  default     = "prod"
}

variable "app_config_name" {
  description = "Label (prod/dev/blue/green)"
  type        = string
  default     = "incident-appcfg"
}

variable "app_config_enabled" {
  description = "Enable App Configuration at runtime"
  type        = bool
  default     = true
}

variable "app_config_cache_seconds" {
  description = "TTL for AppConfig cache"
  type        = number
  default     = 90
}

variable "stabilization_delay" {
  description = "stabilization delשy of the app config"
  type        = string
  default     = "90s"
}
