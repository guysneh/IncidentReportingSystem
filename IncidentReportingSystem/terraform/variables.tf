variable "resource_group_name" {
  description = "The name of the resource group"
  type        = string
  default     = "incident-rg"
}

variable "location" {
  description = "Azure location"
  type        = string
  default     = "westeurope"
}
