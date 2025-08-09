variable "name" { type = string }
variable "resource_group_name" { type = string }
variable "location" { type = string }
variable "sku_name" { type = string }
variable "worker_count" { 
     type = number
     default = 1 
     }
variable "default_tags" { type = map(string) }
