# BookifyHotel Docker and Global Deployment Guide

This guide prepares `Project(DEPI)` for containerized local development and global deployment.

## What is Included

- `Dockerfile`: multi-stage .NET 9 build, non-root runtime user, built-in health check.
- `docker-compose.yml`: app + SQL Server 2022 + Redis, startup ordering, health checks, persistent volumes.
- `docker-compose.prod.yml`: production-oriented override (resource limits, logging, restart policy).
- `.env.template`: all required environment variables with secure defaults/placeholders.
- `fly.toml`: Fly.io app config with health probes and production runtime defaults.
- `.github/workflows/docker-deploy.yml`: build/test/push image and deploy to Fly.io from `main`.
- `init-db.sh`: waits for SQL Server and applies EF Core migrations.
- `Controllers/HealthCheckController.cs`: liveness (`/health/live`) and readiness (`/health/ready`) endpoints.

## 1) Prerequisites

- Docker Desktop (or Docker Engine + Compose v2)
- .NET SDK 9.x (for local migration/script workflows)
- Fly CLI (`flyctl`) for global deployment
- GitHub repository with Actions enabled

## 2) Environment Setup

1. Copy template:
   - `cp .env.template .env` (Linux/macOS)
   - `copy .env.template .env` (Windows CMD)
2. Fill **all secret values** in `.env`:
   - `SQL_PASSWORD`
   - `REDIS_PASSWORD`
   - `STRIPE_SECRET_KEY`, `STRIPE_WEBHOOK_SECRET`
   - `ADMIN_PASSWORD`, `USER_PASSWORD`

Do not commit `.env` to git.

## 3) Run Locally with Containers

From `Project(DEPI)`:

```bash
docker compose up --build -d
```

Check health:

```bash
curl http://localhost:8080/health/live
curl http://localhost:8080/health/ready
```

Stop:

```bash
docker compose down
```

Stop and remove persistent volumes:

```bash
docker compose down -v
```

## 4) Run Production Profile Locally

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

This adds production-style restart behavior, log rotation, and service resource limits.

## 5) Database Migrations

`init-db.sh` can be used in CI/CD or an ops container to safely wait for SQL and run EF migrations:

```bash
chmod +x init-db.sh
./init-db.sh
```

Required variables: `SQL_PASSWORD`; optional: `DB_HOST`, `DB_PORT`, `SQL_DATABASE`, `SQL_USER`.

## 6) Global Deployment on Fly.io

### Initial Setup

```bash
flyctl auth login
flyctl apps create bookifyhotel
```

Create managed Postgres/Redis equivalents if you later move away from containerized SQL/Redis on a single host. For now, this setup expects connection strings and secrets provided via env vars.

### Set Secrets

```bash
flyctl secrets set \
  ConnectionStrings__DefaultConnection="Server=<host>,1433;Database=BookifyHotelDB;User Id=sa;Password=<password>;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=true" \
  ConnectionStrings__Redis="<redis-host>:6379,password=<redis-password>,abortConnect=false" \
  Redis__ConnectionString="<redis-host>:6379,password=<redis-password>,abortConnect=false" \
  Stripe__PublishableKey="<pk_live>" \
  Stripe__SecretKey="<sk_live>" \
  Stripe__WebhookSecret="<whsec>" \
  DefaultUsers__Admin__Email="admin@bookifyhotel.com" \
  DefaultUsers__Admin__Password="<strong-password>" \
  DefaultUsers__User__Email="user@bookifyhotel.com" \
  DefaultUsers__User__Password="<strong-password>"
```

### Deploy

```bash
flyctl deploy --config "fly.toml"
```

### Multi-region Rollout

Primary region is Frankfurt (`fra`). Scale to Amsterdam and Singapore:

```bash
flyctl scale count 3
flyctl machine clone <machine-id> --region ams
flyctl machine clone <machine-id> --region sin
```

Use Fly load balancing + geo-routing for global users.

## 7) GitHub Actions CI/CD

Workflow file: `.github/workflows/docker-deploy.yml`

Pipeline steps:
- Restore/build/test .NET app
- Build and push Docker image to GHCR
- Deploy to Fly.io on `main` branch

Required repository secrets:
- `FLY_API_TOKEN`

Optional: if pushing to another registry, add credentials and adjust workflow.

## 8) Security Checklist

- Keep all secrets in environment variables or platform secret stores.
- Rotate Stripe and DB credentials regularly.
- Use HTTPS everywhere (Fly config forces HTTPS).
- Replace default seeded user passwords immediately after first deployment.
- Prefer managed/global database and cache tiers for true multi-region resilience.

## 9) Troubleshooting

- `503` on `/health/ready`: check SQL/Redis connectivity and passwords.
- SQL container unhealthy: verify `SQL_PASSWORD` meets SQL Server complexity rules.
- Stripe errors: confirm live keys are used in production and webhook secret matches endpoint.
- Startup failure about connection string: ensure `ConnectionStrings__DefaultConnection` is set.
