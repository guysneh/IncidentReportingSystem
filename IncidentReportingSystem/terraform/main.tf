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

module "app_service_plan" {
  source              = "./modules/app_service_plan"
  name                = "incident-app-plan"
  location            = var.location
  resource_group_name = module.resource_group.name
  tags                = var.default_tags
}

module "app_service" {
  source              = "./modules/app_service"
  name                = "incident-api"
  location            = var.location
  resource_group_name = module.resource_group.name
  app_service_plan_id = module.app_service_plan.id
  tags                = var.default_tags
}
