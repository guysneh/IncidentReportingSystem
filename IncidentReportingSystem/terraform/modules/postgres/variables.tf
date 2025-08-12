variable "postgresql_server_name" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "db_admin_username" {
  description = "Admin username for PostgreSQL"
  type        = string
}

# Optional override; leave null in normal cases (we auto-generate)
variable "db_admin_password" {
  description = "Override admin password (null = auto-generate)"
  type        = string
  sensitive   = true
  default     = null
}

variable "public_network_access_enabled" {
  type = bool
}

variable "allow_all_azure" {
  type    = bool
  default = false
}

variable "tags" {
  type    = map(string)
  default = {}
}

variable "key_vault_id" {
  description = "Key Vault resource ID to store the admin password"
  type        = string
}

variable "admin_password_secret_name" {
  description = "Secret name for the admin password in Key Vault"
  type        = string
  default     = "PostgresAdminPassword"
}
