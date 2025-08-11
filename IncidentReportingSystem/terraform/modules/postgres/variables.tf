variable "postgresql_server_name" {
  description = "The name of the PostgreSQL server"
  type        = string
}

variable "location" {
  description = "Azure region"
  type        = string
}

variable "resource_group_name" {
  description = "The name of the resource group"
  type        = string
}

variable "db_admin_username" {
  description = "Admin username for the PostgreSQL server"
  type        = string
}

variable "db_admin_password" {
  description = "Admin password for the PostgreSQL server"
  type        = string
  sensitive   = true
}

variable "tags" {
  description = "Common resource tags"
  type        = map(string)
}

variable "public_network_access_enabled" {
  type    = bool
  default = true
}

variable "allow_all_azure" {
  type    = bool
  default = false
}
