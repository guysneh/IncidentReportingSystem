variable "name" {
  type        = string
  description = "Name of the App Service (Web App)"
}

variable "location" {
  type        = string
  description = "Azure location"
}

variable "resource_group_name" {
  type        = string
  description = "Resource group where the App Service is deployed"
}

variable "app_service_plan_id" {
  type        = string
  description = "ID of the App Service Plan"
}

variable "tags" {
  type        = map(string)
  description = "Common resource tags"
}

variable "app_settings" {
  description = "App settings for the Web App"
  type        = map(string)
  default     = {}
}

variable "key_vault_id" {
  type        = string
  description = "Key Vault ID to grant the Web App identity access to secrets"
  default     = null
}

variable "always_on" {
  type    = bool
  default = true
}

variable "extra_app_settings" {
  type    = map(string)
  default = {}
}

variable "base_app_settings" {
  type    = map(string)
  default = {}
}

variable "health_check_path" {
  description = "Health check endpoint path exposed by the Web App."
  type        = string
  default     = "/health"
}

variable "health_check_eviction_time_in_min" {
  description = "Minutes before evicting an instance failing health checks."
  type        = number
  default     = 5
}