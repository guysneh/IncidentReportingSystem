variable "subscription_id" { type = string }
variable "tenant_id" { type = string }
variable "location" {
  type    = string
  default = "northeurope"
}
variable "resource_group_name" { type = string }
variable "app_service_plan_name" {
  type    = string
  default = ""
}
variable "ui_app_name" { type = string }
variable "api_base_url" {
  type        = string
  description = "e.g. https://api-staging.example.com/api/v1/"
}

# Tags
variable "tags" {
  type    = map(string)
  default = { env = "production", system = "incident-ui" }
}
