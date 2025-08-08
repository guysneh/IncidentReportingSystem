module "resource_group" {
  source              = "./modules/resource_group"
  resource_group_name = var.resource_group_name
  location            = var.location
  default_tags        = var.default_tags
}

module "postgres" {
  source = "./modules/postgres"
  postgresql_server_name = "incident-db"
  location               = var.location
  resource_group_name    = module.resource_group.name
  db_admin_username      = var.db_admin_username   
  db_admin_password      = var.db_admin_password  
  tags                   = var.default_tags
}

