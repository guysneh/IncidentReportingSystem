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
