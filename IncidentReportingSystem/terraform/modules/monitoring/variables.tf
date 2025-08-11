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
