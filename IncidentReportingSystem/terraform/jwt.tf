resource "random_password" "jwt_secret" {
  length      = var.jwt_secret_length
  special     = false
  min_upper   = 2
  min_lower   = 2
  min_numeric = 2
}
