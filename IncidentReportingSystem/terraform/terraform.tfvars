name_prefix = "incident"
tags = {
  owner = "Guy Sne"
  env   = "dev"
}

db_admin_username = "incident_admin"

jwt_issuer   = "https://incident-api.azurewebsites.net"
jwt_audience = "incident-api"

app_name        = "Incident API"
api_basepath    = "/api"
api_version     = "v1"
app_config_name = "incident-appcfg"

demo_enable_config_probe = false
demo_probe_auth_mode     = "Admin"
action_group_email       = "guysneh@gmail.com"

subscription_id         = "27406235-5e35-4236-9a1a-054b917fb75c"
tenant_id               = "1d4c9c59-34e3-48b9-a9cc-3e2a46828136"
ci_role_assignment_name = "34717617-5922-4c36-b9d5-2711f21af8cc"

telemetry_sample_ratio = "0.10"