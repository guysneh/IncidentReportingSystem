terraform {
  backend "azurerm" {
    resource_group_name  = "irs-tfstate-rg"
    storage_account_name = "irstfstatel8m3q6p"
    container_name       = "tfstate"
    key                  = "incident/terraform.tfstate"
    use_azuread_auth     = true
  }
}
