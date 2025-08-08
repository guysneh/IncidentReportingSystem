# Incident Reporting System - Azure Infrastructure

This Terraform configuration sets up the cloud infrastructure for the **Incident Reporting System** in Microsoft Azure.

## 🌍 Modules and Structure

### Root Module
- Defines the provider and subscription.
- Uses modules for resource group provisioning.
- Configures budget alerts.

### Modules
#### `modules/resource_group`
- Creates a resource group with consistent naming and tags.
- Supports location and naming via input variables.

## ⚙️ Features

- 📁 Resource Group: Central container for Azure resources.
- 💰 Budget Alert: Monthly budget with warning and critical thresholds.
- 🔒 Secure Configuration: Subscription ID and secrets are handled via local variables and not committed to Git.

## 🚀 Getting Started

1. Configure your `terraform.tfvars` file or pass variables securely (e.g., `subscription_id`).
2. Run:
    ```bash
    terraform init
    terraform plan
    terraform apply
    ```

## 📦 Variables

Key variables:
- `subscription_id` (required, passed securely)
- `resource_group_name`
- `location`
- `budget_amount`, `budget_threshold_warn`, `budget_threshold_crit`
- `notification_emails`
- `default_tags`

## 🛑 Git Hygiene

> ✅ `.terraform.tfstate`, `terraform.tfvars`, and sensitive configuration files should **NOT** be committed to Git.

## ✍️ Author

**Guy Sne**  
`guysneh@gmail.com`

---

Feel free to extend this file as more resources and modules are added.