variable "location" {
  description = "Azure region for the tfstate RG/Storage"
  type        = string
  default     = "northeurope"
}

variable "default_tags" {
  description = "Common tags"
  type        = map(string)
  default     = {}
}
