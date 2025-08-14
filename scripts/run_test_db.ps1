docker run --name irs_test -d -p 5444:5432 `
  -e POSTGRES_USER=testuser `
  -e POSTGRES_PASSWORD=testpassword `
  -e POSTGRES_DB=testdb `
  postgres:15-alpine