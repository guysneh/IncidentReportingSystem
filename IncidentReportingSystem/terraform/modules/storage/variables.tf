variable "name" {
  description = "Base name for the Storage Account (will be normalized)"
  type        = string
}

variable "location" {
  description = "Azure location"
  type        = string
}

variable "resource_group_name" {
  description = "Resource group name"
  type        = string
}

variable "container_name" {
  description = "Blob container name for attachments"
  type        = string
  default     = "attachments"
}

variable "default_tags" {
  description = "Default tags for all resources"
  type        = map(string)
}
