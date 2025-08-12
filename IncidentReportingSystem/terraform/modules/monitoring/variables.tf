variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "name_prefix" {
  type = string
  # e.g. "incident"
}

variable "tags" {
  type    = map(string)
  default = {}
}

variable "log_analytics_retention_days" {
  type    = number
  default = 30
}

variable "ai_retention_days" {
  type        = number
  description = "Application Insights retention in days"
  default     = 30

  validation {
    condition     = var.ai_retention_days >= 30 && var.ai_retention_days <= 730
    error_message = "ai_retention_days must be between 30 and 730."
  }
}

variable "action_group_name" {
  type = string
}
variable "action_group_email" {
  type = string
}
