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
variable "label" {
  description = "Label for App Configuration keys."
  type        = string
  default     = "prod"
}

variable "stabilization_delay" {
  description = "Delay to allow the store to be fully ready before writing keys."
  type        = string
  default     = "90s"
}
