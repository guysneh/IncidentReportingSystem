variable "name" { type = string }
variable "location" { type = string }
variable "resource_group_name" { type = string }
variable "tags" { type = map(string) }
variable "secrets" {
  type        = map(string)
  description = "Map of secrets to create in KV"
}
