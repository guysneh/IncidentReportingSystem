variable "resource_group_name" {
  type        = string
  description = "The name of the resource group"
}

variable "location" {
  type        = string
  description = "Azure location for the resource group"
}

variable "default_tags" {
  type        = map(string)
  description = "Tags to be applied to the resource group"
}
