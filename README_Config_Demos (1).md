# Operational & Configuration Demo Guide (Non-Swagger Routes)

> Paste this section into your main `README.md` (recommended location: after the “Live demo” section), or keep it as a separate doc.

## Table of Contents
- [Operational & Diagnostics Endpoints (non-Swagger)](#operational--diagnostics-endpoints-non-swagger)
  - [Health](#health)
  - [Configuration Diagnostics](#configuration-diagnostics)
- [Feature Flags & Swagger Gating](#feature-flags--swagger-gating)
- [Configuration Demo Playbook (Sentinel, Cache/TTL, Refresh)](#configuration-demo-playbook-sentinel-cachettl-refresh)
  - [Prerequisites](#prerequisites)
  - [Baseline](#baseline)
  - [A) Demonstrate Cache & TTL](#a-demonstrate-cache--ttl)
  - [B) Demonstrate Sentinel-based refresh](#b-demonstrate-sentinel-based-refresh)
  - [C) Demonstrate Force Refresh (bypass TTL)](#c-demonstrate-force-refresh-bypass-ttl)
  - [CLI (Optional)](#cli-optional)
- [Rate Limiting (FYI)](#rate-limiting-fyi)
- [Local / Offline Mode (no App Configuration)](#local--offline-mode-no-app-configuration)
- [JWT Key Rotation with `kid` (Optional, Design Notes)](#jwt-key-rotation-with-kid-optional-design-notes)
  - [Current State](#current-state)
  - [Recommended Rotation Model](#recommended-rotation-model)
  - [Minimal Validation Snippet (Conceptual)](#minimal-validation-snippet-conceptual)
  - [Rotation Timeline](#rotation-timeline)
- [Troubleshooting](#troubleshooting)

---

## Operational & Diagnostics Endpoints (non-Swagger)

> These routes are intentionally **not** exposed in Swagger UI, so they remain simple operational probes.

### Health
- `GET /health/live`  
  Liveness probe. Returns `{ "status": "ok" }`. **No auth** required.

- `GET /health`  
  Readiness/health checks (e.g., PostgreSQL via Npgsql). **No auth** required.

### Configuration Diagnostics
- `GET /diagnostics/config`  
  Returns the current view of key config values:
  ```json
  {
    "AppConfigEnabled": "true",
    "Label": "prod",
    "Sentinel": "2",
    "CacheSeconds": "90",
    "SampleRatio": "1.0"
  }
  ```

- `GET /diagnostics/config/refresh-state`  
  Shows when options were **last re-bound** (UTC). Useful to verify a refresh occurred:
  ```json
  { "lastRefreshUtc": "2025-08-20T18:12:05.1234567Z" }
  ```

- `POST /diagnostics/config/force-refresh`  
  Forces an immediate refresh attempt (bypasses the configured TTL). Handy for demos/tests:
  ```json
  { "forced": true, "atUtc": "2025-08-20T18:12:34.5678901Z" }
  ```

> Tip: All diagnostics endpoints are anonymous and excluded from Swagger (`ExcludeFromDescription`).


## Feature Flags & Swagger Gating

We use **Azure App Configuration Feature Flags** to gate Swagger UI when App Configuration is active.

- Feature flag name: **`EnableSwaggerUI`**
- Behavior:
  - If App Configuration is **inactive** → Swagger UI is **always** accessible (local dev safety).
  - If App Configuration is **active** → access to `/swagger` is allowed **only when** `EnableSwaggerUI = true`.

**To demo:**
1. Ensure App Configuration is active (see `/diagnostics/config` → `AppConfigEnabled=true`).
2. In **App Configuration → Feature manager**, toggle **`EnableSwaggerUI`** to **false** (match the same Label).
3. Wait for the Feature Flags cache TTL (see `AppConfig__CacheSeconds`) **or** call:
   ```
   POST /diagnostics/config/force-refresh
   ```
4. Browse `/swagger` → you should get **404 Not Found**.
5. Toggle back to **true**, wait TTL or force refresh → `/swagger` works again.

> Note: Feature flags are cached with the same TTL you configured (`AppConfig__CacheSeconds`), and also refresh on force-refresh.


## Configuration Demo Playbook (Sentinel, Cache/TTL, Refresh)

The app uses:
- **In-memory cache** for key-values with TTL = `AppConfig__CacheSeconds`.
- **Sentinel key** `AppConfig:Sentinel` to trigger a **hot refresh** of all watched keys (checked when TTL elapses).
- **Force-refresh** endpoint to bypass TTL during demos/tests.

### Prerequisites
- App Settings (in App Service) were set by Terraform:
  - `AppConfig__Enabled=true`
  - `AppConfig__Endpoint=https://<your-appcfg>.azconfig.io`
  - `AppConfig__Label=prod`
  - `AppConfig__CacheSeconds=90` (example)
- Keys exist in App Configuration (with the **same Label**):
  - `AppConfig:Sentinel = "1"`
  - `MyAppSettings:SampleRatio = "1.0"`

### Baseline
```
GET /diagnostics/config
```
Expect something like:
```json
{ "AppConfigEnabled": "true", "Label": "prod", "Sentinel": "1", "CacheSeconds": "90", "SampleRatio": "1.0" }
```

### A) Demonstrate **Cache & TTL**
1. In App Configuration (label `prod`), change:
   - `MyAppSettings:SampleRatio` → `0.7`
   - **Do not** change the sentinel yet.
2. Immediately call:
   ```
   GET /diagnostics/config
   ```
   You should still see `SampleRatio = 1.0` (cached).
3. Wait for **TTL** (e.g., 90s) → call again → you’ll see `0.7`.

> This proves the in-memory cache holds values until TTL elapses.

### B) Demonstrate **Sentinel-based refresh**
1. In App Configuration (label `prod`), **bump**:
   - `AppConfig:Sentinel` → from `1` to `2`
2. After TTL elapses, the next request will notice the sentinel change and **refresh** values.
3. Call:
   ```
   GET /diagnostics/config
   GET /diagnostics/config/refresh-state
   ```
   You should see the new `Sentinel = "2"` and an updated `lastRefreshUtc`.

> Sentinel is evaluated when the TTL window expires. Use **force refresh** to bypass TTL during demos.

### C) Demonstrate **Force Refresh (bypass TTL)**
1. Change a value in App Configuration (e.g., `SampleRatio`).
2. Immediately call:
   ```
   POST /diagnostics/config/force-refresh
   GET  /diagnostics/config
   ```
   You should see the new value **right away**, without waiting for TTL.

### CLI (Optional)
You can also change keys via Azure CLI:
```bash
# Update SampleRatio (label=prod)
az appconfig kv set --name <APP_CONFIG_NAME> --key "MyAppSettings:SampleRatio" --value "0.7" --label "prod"

# Bump the sentinel (label=prod)
az appconfig kv set --name <APP_CONFIG_NAME> --key "AppConfig:Sentinel" --value "2" --label "prod"
```


## Rate Limiting (FYI)

The API uses a simple **fixed-window** global rate limiter (e.g., 10 requests / 10 seconds, queue limit 5).  
If you fire more than the allowed rate, you will get **HTTP 429 Too Many Requests**.

**Quick check (bash loop):**
```bash
for i in {1..20}; do curl -s -o /dev/null -w "%{http_code}\n" https://<your-app>/health/live; done
# You should see some 200s and some 429s within a 10-second window.
```


## Local / Offline Mode (no App Configuration)

- Local dev and Docker compose use:
  - `AppConfig__Enabled=false`
  - Optional defaults in `appsettings.Development.json` or environment variables
- Diagnostics routes work the same:
  - `GET /diagnostics/config` will show `AppConfigEnabled=false`
  - You can still exercise health endpoints and any feature defaults you set locally


## JWT Key Rotation with `kid` (Optional, Design Notes)

> Current code signs/validates JWT using a **single symmetric secret** (`Jwt:Secret`). This section documents the **recommended** approach to move toward key rotation with **`kid`**. It’s optional and can be implemented later without breaking clients.

### Current State
- Signing: HS256 with one key from configuration (`Jwt:Secret`).
- Validation: one `IssuerSigningKey` configured at startup.
- Rotation impact: changing the secret invalidates all existing tokens immediately.

### Recommended Rotation Model
- Keep **two signing keys** active during a rotation window:
  - New tokens are signed with the **current key** (e.g., `kid = k2`).
  - Validation accepts both **k1** and **k2** (based on the token header `kid`).
- Store keys in **Key Vault** and reference their values via **App Configuration**:
  - `Jwt:SigningKeys:k1` → Key Vault secret (old)
  - `Jwt:SigningKeys:k2` → Key Vault secret (new/current)
  - `Jwt:ActiveKid`      → `"k2"`
- Use App Configuration **sentinel** + TTL so the app learns about key changes **without restarts**.

### Minimal Validation Snippet (Conceptual)
```csharp
// Pseudo: wiring multiple keys by 'kid' (to be added when you move to rotation)
var keysByKid = new ConcurrentDictionary<string, SecurityKey>(StringComparer.OrdinalIgnoreCase);

// On config refresh (e.g., via IOptionsMonitor or manual reload), rebuild the map:
keysByKid["k1"] = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:SigningKeys:k1"]));
keysByKid["k2"] = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:SigningKeys:k2"]));

var tvp = new TokenValidationParameters
{
    // ...
    ValidateIssuerSigningKey = true,
    IssuerSigningKeyResolver = (token, secToken, kid, parameters) =>
        (kid != null && keysByKid.TryGetValue(kid, out var key))
            ? new[] { key } : Array.Empty<SecurityKey>()
};
// The issuer uses cfg["Jwt:ActiveKid"] to choose which key to sign with.
```

### Rotation Timeline
1. **Prepare**: Generate a new key, add it as `k2` in Key Vault/App Config; set `Jwt:ActiveKid = "k2"` for the issuer.
2. **Overlap**: Keep **k1 & k2** available for validation (clients with old tokens continue to work).
3. **Expire old tokens**: Wait until the longest token lifetime has passed (e.g., 60 minutes).
4. **Retire**: Remove `k1` from validation and delete its secret from Key Vault.

> Benefit: No restart and no mass invalidation of in-flight tokens.


## Troubleshooting

### App Configuration not applied
- `/diagnostics/config` shows `AppConfigEnabled=false` or missing endpoint:
  - Verify App Service **App Settings**: `AppConfig__Enabled`, `AppConfig__Endpoint`, `AppConfig__Label`, `AppConfig__CacheSeconds`.
  - Check **RBAC**: API’s Managed Identity must have **App Configuration Data Reader** on the store.
  - If using private networking/firewall, ensure the Web App can reach the endpoint.

### Values still not refreshing
- Ensure the key changes are under the **same Label** used by the app.
- Remember: refresh happens when **TTL elapses**; either wait for `AppConfig__CacheSeconds` or call `POST /diagnostics/config/force-refresh`.
- Confirm the **sentinel key** `AppConfig:Sentinel` was bumped (and with the same label).

### Swagger shows 404
- When App Configuration is active, `/swagger` depends on the **`EnableSwaggerUI`** feature flag.
- Toggle the flag to `true`, then wait for TTL or **force refresh**.

### 401/403 after changing roles/flags
- Tokens issued **before** the change carry old claims until they **expire**. Re-authenticate to get a fresh token.

### Rate limiting 429
- Expected under burst traffic; reduce call rate or tune the limiter settings if needed.

### Local dev
- Use `AppConfig__Enabled=false` (or provide environment credentials & endpoint to talk to real App Config).
- `GET /diagnostics/config` will clearly show the current mode.
