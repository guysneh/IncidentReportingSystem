docker run --name irs_dev -d -p 5433:5432 `
  -e POSTGRES_USER=incident_user `
  -e POSTGRES_PASSWORD=incident_pass `
  -e POSTGRES_DB=incident_db `
  postgres:15-alpine