resource "random_password" "postgres_admin" {
  length           = 24
  special          = true
  override_special = "_%@"
  min_upper        = 2
  min_lower        = 4
  min_numeric      = 2
}
