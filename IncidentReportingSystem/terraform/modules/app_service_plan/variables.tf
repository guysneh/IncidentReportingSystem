variable "name" {
  type        = string
  description = "The name of the App Service Plan"
}

variable "location" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "tags" {
  type = map(string)
}
