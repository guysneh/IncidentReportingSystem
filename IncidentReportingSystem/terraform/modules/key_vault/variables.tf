variable "name" { type = string }
variable "location" { type = string }
variable "resource_group_name" { type = string }
variable "tags" { type = map(string) }
variable "secrets" {
  type        = map(string)
  description = "Map of secrets to create in KV"
}
variable "ci_principal_object_id" { type = string }
variable "ci_role_assignment_name" {
  type        = string
  description = "Existing RBAC assignment GUID for CI on Key Vault (to prevent recreation)."
}
