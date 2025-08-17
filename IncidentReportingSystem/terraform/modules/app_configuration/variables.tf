variable "name" {
  type        = string
  description = "App Configuration resource name"
}

variable "resource_group_name" {
  type        = string
  description = "Target resource group name"
}

variable "location" {
  type        = string
  description = "Azure location"
}

variable "tags" {
  type    = map(string)
  default = {}
}

variable "key_vault_uri" {
  type        = string
  description = "Key Vault base URI (e.g., https://kv-name.vault.azure.net/)"
}

variable "webapp_principal_id" {
  type        = string
  description = "Managed Identity principal ID of the Web App (for data-plane access)"
}

# App-level configuration (non-secrets)
variable "app_name" {
  type    = string
  default = "Incident API"
}

variable "api_basepath" {
  type    = string
  default = "/api"
}

variable "api_version" {
  type    = string
  default = "v1"
}

variable "enable_swagger" {
  type    = bool
  default = true
}

# Demo probe controls
variable "demo_enable_config_probe" {
  type    = bool
  default = false
}

variable "demo_probe_auth_mode" {
  type    = string
  default = "Admin"
  validation {
    condition     = contains(["Admin", "Anonymous"], var.demo_probe_auth_mode)
    error_message = "demo_probe_auth_mode must be 'Admin' or 'Anonymous'."
  }
}

# Secret names inside Key Vault (used by App Configuration KV references)
variable "db_conn_secret_name" {
  type    = string
  default = "PostgreSqlRuntimeConnectionString"
}

variable "jwt_issuer_secret_name" {
  type    = string
  default = "jwt-issuer"
}

variable "jwt_audience_secret_name" {
  type    = string
  default = "jwt-audience"
}

variable "jwt_secret_secret_name" {
  type    = string
  default = "jwt-secret"
}

variable "ai_connection_string" {
  type    = string
  default = ""
}

variable "feature_enable_demo_banner_default" {
  type    = bool
  default = false
}
