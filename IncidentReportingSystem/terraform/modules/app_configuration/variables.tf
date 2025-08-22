variable "name" {
  description = "App Configuration name."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group name."
  type        = string
}

variable "location" {
  description = "Azure region."
  type        = string
}

variable "tags" {
  description = "Default tags."
  type        = map(string)
}

variable "app_name" {
  description = "Application name."
  type        = string
}

variable "api_basepath" {
  description = "API base path (e.g., /api)."
  type        = string
}

variable "api_version" {
  description = "API version (e.g., v1)."
  type        = string
}

variable "enable_swagger" {
  description = "Enable Swagger UI flag."
  type        = bool
}

variable "demo_enable_config_probe" {
  description = "Enable demo config probe flag."
  type        = bool
}

variable "stabilization_delay" {
  description = "Delay to allow the store to be fully ready before writing keys."
  type        = string
  default     = "90s"
}

variable "app_config_name" {
  type        = string
  description = "App Configuration resource name"
  default     = "incident-appcfg"
}

variable "app_config_label" {
  description = "Label to scope App Configuration keys/features (e.g. prod)"
  type        = string
  default     = "prod"
}

variable "default_tags" {
  description = "Default tags to apply to resources"
  type        = map(string)
}

variable "web_app_name" {
  description = "Name of the Web App that will consume the App Configuration"
  type        = string
}

variable "telemetry_sample_ratio" {
  description = "Telemetry sample ratio for diagnostics"
  type        = string
  default     = "0.10"
}

variable "app_config_cache_seconds" {
  description = "TTL for AppConfig cache"
  type        = number
  default     = 90
}