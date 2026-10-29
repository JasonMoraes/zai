# Login feature — `POST /api/auth/login`

## Decisions
- **DB:** PostgreSQL via EF Core (Npgsql). Local dev uses the Homebrew Postgres 17 already running on `localhost:5432`, new database `zai`.
- **Admin account:** a single admin, seeded at startup from config (`Admin:Username`, `Admin:Password`) if it doesn't exist yet. Only the password **hash** is stored (ASP.NET Core `PasswordHasher`, PBKDF2).
- **Token:** JWT signed with HS256, `tokenType: "Bearer"`, `expiresIn: 3600`. JwtBearer authentication is registered too, so future endpoints can call `.RequireAuthorization()`.
- **Errors:** `application/problem+json` (RFC 7807 ProblemDetails) for 400 and 401.
- **Migrations:** applied automatically at startup. That's simplest on free hosting, where there's no separate deploy step.
- **Config:** dev values (connection string, admin login, JWT key) go in `appsettings.Development.json`, local only. Production takes everything from environment variables, and the app refuses to start if the JWT key or admin credentials are missing.

## Steps
- [x] Add packages (all 8.0.x): `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.Extensions.Identity.Core`
- [x] `Data/`: `AppDbContext`, `AdminUser` (Id, Username unique, PasswordHash), `AdminSeeder`
- [x] `Features/Auth/`: `LoginRequest`, `LoginResponse`, `TokenService`, `AuthEndpoints` (`MapAuthEndpoints`)
- [x] Login logic:
  - 400 if the body is missing, malformed JSON, or `username`/`password` is empty
  - 401 if the username is unknown or the password is wrong. Same response either way, and a dummy hash check runs for unknown users so response timing doesn't reveal which usernames exist
  - 200 with `{ accessToken, tokenType: "Bearer", expiresIn }`
- [x] `AddProblemDetails()` so framework 400s and 401s are problem+json too
- [x] Swagger: `auth` tag, summary, document 200/400/401 with `application/problem+json` (Authorize button postponed until the first protected endpoint exists)
- [x] Move `/api/health` into the same endpoint-group style (behaviour unchanged)
- [x] Create the `InitialCreate` migration
- [x] Verify with curl: 200 with a valid JWT (decode and check claims and expiry), 401 wrong password, 401 unknown user, 400 empty fields, 400 malformed JSON, 400 no body; check the seeded row in the DB holds a hash; check the Swagger doc
- [x] Code review via subagent

## Known contract gaps (OpenAPI 3.0 / Swashbuckle limits)
- `tokenType: const Bearer` will be generated as a plain `string`
- `password: format: password` won't be emitted unless a schema filter adds it (I can add one)
- The `Problem` schema wasn't pasted, so I'm assuming the standard RFC 7807 fields (`type`, `title`, `status`, `detail`, `instance`)

## Review
**Status:** done and verified locally against Postgres 17.

**Verified responses (curl):**
| Case | Result |
|---|---|
| Valid credentials | 200; JWT has `sub`, `unique_name`, `iss`, `aud`, and `exp − iat = 3600` |
| Wrong password / unknown user | 401 problem+json, identical body, similar timing (~40 ms) |
| Empty / missing fields, NUL char, username > 100, password > 256 | 400 validation problem+json |
| Malformed JSON, wrong types, no body | 400 problem+json |
| `text/plain` body | 415 problem+json |
| Production with missing `Jwt__SigningKey` or `Admin__Password` < 12 chars | app refuses to start |

**Deviations from plan:**
- `RouteHandlerOptions.ThrowOnBadRequest = false`. Without it, Development turned bad bodies into **500**s (found during testing).
- The Swagger Authorize button is postponed. Without a protected endpoint it would do nothing.

**Code review (subagent). Applied:**
- A NUL character in the username caused a 500 from Postgres (reproduced, now 400)
- Length caps on username and password
- JWT validation pins HS256, `NameClaimType = unique_name`, `ClockSkew` 30 s
- Empty connection string now fails fast
- Admin seed password must be at least 12 characters (dev password is now `LocalAdmin123!`)
- Failed logins are logged as warnings (username only)

**Code review. Not applied (judged low value for now):**
- Options validation runs at `app.Run()`, after migrations. It still fails fast, just after the DB has been touched.
- The dummy hash uses a separate `PasswordHasher` instance. That only matters if hasher options are customised later.
- No `WWW-Authenticate` header on the login 401. That's normal for a credentials-in-body endpoint.

**Things to know:**
- The admin is seeded only when the table is empty. Changing `Admin__Password` later does nothing; you'd delete the row or add a change-password feature.
- JWT bearer validation is configured but not exercised yet, because there's no protected endpoint. Test it with the first one.
- Free-tier Postgres may need `SSL Mode=Require` in the connection string, and possibly `EnableRetryOnFailure()` for cold starts.

---

# Refactor: layered structure (controllers + services)

**Decisions (with user):** one project with layer folders (no Clean Architecture split); services use `AppDbContext` directly (no repositories).

```
backend/Api/
├── Controllers/     AuthController, HealthController — thin: HTTP in/out only
├── Services/        IAuthService/AuthService, ITokenService/TokenService, AccessToken
├── Dtos/            Auth/LoginRequest+LoginResponse, Health/HealthResponse — API contract shapes
├── Entities/        AdminUser — persistence model
├── Data/            AppDbContext, DatabaseInitializer, Migrations/
├── Configuration/   JwtOptions, AdminOptions — bound + validated config
├── Validation/      FieldLimits — limits shared by DB schema, DTOs and config
├── Extensions/      ServiceCollectionExtensions — DI registration grouped by concern
└── Program.cs       pipeline only
```

- [x] Minimal API endpoints → `[ApiController]` controllers; validation via DataAnnotations on DTOs
- [x] Business logic → `AuthService`; token creation → `TokenService` (returns `AccessToken`, not the HTTP DTO; controller maps to `LoginResponse`)
- [x] Migration snapshot type names updated; `has-pending-model-changes` → none
- [x] Re-verified every scenario (same results as before, incl. problem+json content types, 415s, fail-fast config)
- [x] Code review (subagent) — applied: `Options` → `Configuration` namespace (it shadowed `Microsoft.Extensions.Options.Options` and would break tests), DTO/config no longer depend on the entity (`FieldLimits`), lazy connection string (test-overridable), single `JwtOptions.Algorithm` constant, consistent `ActionResult<T>` returns, failed-login log sanitised (control chars → `?`, truncated to 32 chars — verified a `\n` username can't forge a log line)

**Gotchas found during the refactor:**
- A class-level `[Produces("application/json")]` forced error responses to `application/json` → removed; content types are declared per `[ProducesResponseType]`.
- MVC's implicit `[Required]` on non-nullable parameters added a noisy `"request": ["The request field is required."]` to malformed-JSON errors → `SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true`. **Every new DTO property must declare `[Required]` explicitly.**

**Review items not applied (noted for later):**
- Rate limiting on `/api/auth/login` (each attempt costs a PBKDF2 hash) — add before going public.
- `Admin__*` config is validated on every boot even after seeding.
- Options registrations live in different `Add*` methods than some consumers (`TokenService` relies on `AddJwtAuthentication`).
- `TokenService` rebuilds signing credentials per call — negligible at this scale.
- Usernames are case-sensitive (`Admin` ≠ `admin`).

---

# Auth endpoints: full contract v2.0.0 

## Endpoints
| Endpoint | Auth | Responses |
|---|---|---|
| `POST /api/auth/login` | none | 200, 400, 401 (exists; error body format changes, see below) |
| `POST /api/auth/logout` | Bearer | 204; 401 |
| `PUT /api/auth/password` | Bearer | 204; 400 bad syntax; 401; **403** wrong current password; **422** new password breaks the rules |

## Cross-cutting changes the contract requires (they affect login too)
1. **Error body format.** The contract's `Problem.errors` is an **array** of `{ field, message }`. Today ASP.NET returns a dictionary `{ "username": ["..."] }`. I'll replace the automatic validation response so every 400 uses the array form. This changes the login 400 body.
2. **406 Not Acceptable** when the `Accept` header can't be satisfied (e.g. only `application/xml`). Turned on globally with `ReturnHttpNotAcceptable`. I'll check it doesn't wrongly turn a client's `Accept: application/json` into a 406 on `problem+json` errors.
3. **401 for a missing or invalid token** must also be `problem+json`. This should already be covered by `UseStatusCodePages`; to be verified.
4. **Swagger** gets the `bearerAuth` security scheme (the contract's name), and a lock icon is shown only on `[Authorize]` endpoints, so the Authorize button finally makes sense.

## Logout: how a token "stops being valid"
A JWT can't be un-issued, so logout needs a server-side denylist:
- Each token gets a unique `jti` claim.
- Logout stores that `jti` plus its expiry in a new `RevokedTokens` table, and deletes expired rows while it's there.
- Every authenticated request checks the table (one indexed lookup). A revoked token gets 401.
- It's stored in the DB rather than memory because free hosts restart often, and an in-memory list would forget revocations.

## Password change rules
- `currentPassword` and `newPassword` are required. Missing, null or wrong type gives **400**.
- A wrong `currentPassword` gives **403**.
- `newPassword` shorter than 8, longer than 256, or equal to the current password gives **422** with `errors: [{ field: "newPassword", message }]`.
- The minimum of 8 becomes one shared limit. The seeded admin password rule drops from 12 to 8 to match.
- **Existing tokens stay valid after a password change.** Revoking all of them, including the one that made the change, is safer, but it's not in the contract and would force a re-login. I can add it if you want.

## Steps
- [x] `ProblemDetails` customisation: validation errors as `[{field, message}]`, and a helper for 422s from services
- [x] `ReturnHttpNotAcceptable = true`
- [x] `jti` claim in tokens; `RevokedToken` entity + migration; check in `JwtBearerEvents.OnTokenValidated`
- [x] `ITokenRevocationService` (revoke + is-revoked + cleanup)
- [x] `AuthService.ChangePasswordAsync` returning a result (Changed / WrongCurrentPassword / invalid new password + reason)
- [x] `AuthController`: `logout`, `password` with `[Authorize]`
- [x] Swagger `bearerAuth` scheme + operation filter
- [x] Verify with curl: logout → same token now 401 (also after a server restart); a second token still works; no token / garbage / expired / wrong signature → 401 problem+json; password: 204 then old password fails login and new one works; 400 / 403 / 422 cases; 406 and 415 cases; login error format
- [x] Code review via subagent

## Review

**Status:** done and verified end-to-end against local Postgres. The test script covers about 60 cases.

**Verified:**
| Area | Result |
|---|---|
| Logout | 204. The same token then gets 401, **also after a server restart**. A second token is unaffected. |
| Bad tokens | None, garbage, expired (correct signature) or wrong signature: 401 problem+json with `WWW-Authenticate: Bearer` |
| Password change | 204, then the old password fails login and the new one works. 400 missing/null/malformed/wrong type. 403 wrong current password. 422 too short, too long (in characters), or same as current. 415 non-JSON. |
| Error format | Every 400/422 has `errors: [{field, message}]`. Swagger documents it as `ApiProblemDetails`. |
| 406 | `Accept: application/xml` gives 406. `Accept: application/json` on a problem+json error still returns the error, not 406. |
| Ordering | No token plus non-JSON body gives **401**, not 415. |
| Other | 405 and 404 are problem+json. Swagger shows `bearerAuth` only on logout and password. |

**Code review (subagent). Applied:**
- Revocation cleanup deleted rows at `exp`, but tokens are accepted until `exp + 30 s` of clock skew, so a logged-out token could come back for up to 30 s. Cleanup now uses the shared `JwtOptions.ClockSkew`.
- 415 came before 401 (`[Consumes]` runs at endpoint matching). It's replaced by limiting the JSON input formatter to `application/json` globally, so the check runs after authorization.
- Whitespace-only passwords were rejected by `[Required]`. Passwords now use `AllowEmptyStrings = true`, and DTO properties default to `null`, so a *missing* field is still 400. (A first attempt without the `null` default made `{}` return 422; the test script caught it.)
- Password length is counted in Unicode code points, as JSON Schema does: 4 emoji is 4 characters, not 8.
- A typed `ApiProblemDetails` replaces the untyped `Extensions["errors"]`, so the `errors` array appears in Swagger.
- Controller 401s use `Challenge()`, so they also carry `WWW-Authenticate`.
- Claim parsing moved to `ClaimsPrincipalExtensions` (invariant culture, `TryParse`).

**Code review. Checked and not reproducible:**
- A `null` JSON body was claimed to give 500. MVC already returns 400.

**Not applied:**
- `detail`/`instance` are not always present. The contract only requires `title` and `status`, and it allows extra fields such as `traceId`.
- `minLength: 8` for `newPassword` is not shown in Swagger (needs a schema filter).
- Logout with `Accept: application/xml` returns 204, because there's no body to negotiate.
- Concurrent password changes are last-write-wins. There's a single admin.

**Still open from earlier:** rate limiting on login; the admin config is required on every boot.

---

# Security requirements check (course guidelines)

| Requirement | Status | How |
|---|---|---|
| Passwords stored with a password-hashing function (e.g. bcrypt) | ✅ | ASP.NET Core Identity `PasswordHasher`: **PBKDF2-HMAC-SHA512, 100,000 iterations, random salt per password**. It's an OWASP-recommended password KDF in the same class as bcrypt. A random-salted dummy hash keeps unknown-user logins equally slow. |
| No raw SQL; parameterised queries or an ORM | ✅ | All data access goes through EF Core. The one hand-written statement (the logout `INSERT … ON CONFLICT`, which was parameterised) is **replaced** by `Add` + `SaveChanges`, catching the unique-key violation from a concurrent duplicate logout. `grep` finds no `ExecuteSql`/`FromSql`/`SqlQuery` outside generated migrations. |
| Safe session/token mechanism (HTTP-only cookie or limited-lifetime tokens) | ✅ | JWT HS256 (key ≥ 32 chars, algorithm pinned), **1 h lifetime**, 30 s clock skew. Revocable on logout through a DB denylist that survives restarts. Expired or wrong-signature tokens are rejected (tested). |
| Authorization of all modifying operations on the server | ✅ | **Secure by default:** `MapControllers().RequireAuthorization()` makes every controller action require a token unless marked `[AllowAnonymous]` (only `health` and `login` are). Verified with a temporary controller with no attributes: 401 without a token, 204 with one. |

**Gotcha found:** the first attempt used a global `FallbackPolicy`. That also put routing's 404/405 responses behind login, so `GET /api/nope` gave **401**. That would break the contract rule that PUT/PATCH/DELETE on measurements return 404/405. Scoping it to controllers fixed this (re-tested: 404/405 are public again).

**For upcoming endpoints:**
- Public GETs (`/api/series`, `/api/measurements`) need `[AllowAnonymous]`.
- `POST /api/measurements` needs `[AllowAnonymous]` plus its own `X-API-Key` check (an admin token must **not** be enough).
- Sensor API keys must be stored as hashes, as the contract says.
- Frontend: keep the token in memory (or `sessionStorage`), not `localStorage`, to limit XSS exposure.

---

# Sensor endpoints

## Endpoints (all admin-only, Bearer)
| Endpoint | Responses |
|---|---|
| `GET /api/sensors` | 200 list (**never** includes API keys); 401 |
| `POST /api/sensors` `{name, seriesId}` | **201** + `Location: /api/sensors/{id}` + body with `apiKey` (shown **only here**); 400; 401; 415; **422** if the series doesn't exist |
| `GET /api/sensors/{id}` | 200 (no key); 401; 404 |
| `DELETE /api/sensors/{id}` | 204, key stops working immediately; 401; 404 |

Response shape: `{ id, name, seriesId, createdAt, lastMeasurementAt }`. `lastMeasurementAt` stays `null` until measurements exist.

## The series dependency
Sensors reference a series, but series don't exist yet. **Proposal:** add the `Series` **entity and table** now, with the contract's fields (`name`, `minValue`, `maxValue`, `color`, `icon`, `unit`), but **no series endpoints**.
- The FK and the 422 "series doesn't exist" check are real from day one.
- The series endpoints later only add a controller and service on top of the same table.
- Until then I test by inserting a series row directly with `psql`.

## API keys
- 32 random bytes from a cryptographic RNG, encoded base64url (43 chars, above the contract's `minLength: 32`).
- Stored as a **SHA-256 hash** with a unique index, never in plain text, as the contract requires.
- Why SHA-256 and not PBKDF2/bcrypt like passwords: slow hashes protect *guessable* human passwords. A 256-bit random key can't be brute-forced anyway. A slow, salted hash would also make every measurement upload scan and re-hash every sensor, because you couldn't look the key up by hash. (This is standard practice for API keys, e.g. GitHub tokens.)
- Checking the key on `POST /api/measurements` belongs to the measurements work. Here I only generate and store it.

## Deletion
- **Hard delete** of the sensor row: the key's hash is gone, so the key can't work.
- When measurements exist, their `sensorId` FK will be `ON DELETE SET NULL`, so "collected results remain" holds.
- Deleting a series will cascade to its sensors (the contract: series deleted "wraz z … przypisanymi czujnikami").

## Validation
- `name` missing, empty, or > 100 chars: **400**. `seriesId` missing or wrong type: **400**.
- `seriesId` well-formed but no such series: **422** with `errors: [{field: "seriesId", …}]`.
- IDs are integers. The contract allows `integer | string` and clients treat them as opaque.

## Steps
- [x] Entities `Series`, `Sensor` + configuration (FK cascade from series, unique index on key hash) + migration
- [x] `IApiKeyGenerator` (generate + hash)
- [x] DTOs `SensorRequest`, `SensorResponse`, `SensorCreatedResponse`
- [x] `ISensorService` / `SensorService` (list, get, create → result with "series not found", delete)
- [x] `SensorsController` with 201 + `Location` via `CreatedAtAction`
- [x] Extend the test script: auth on all four endpoints, 201 + Location + key format, key never returned by GET, hash stored rather than the key, 422 for unknown series, 400s, 404s, delete, 406/415
- [x] Code review via subagent

## Review

**Status:** done and verified end-to-end. The sensors script has about 40 checks, and the 58-check auth suite was re-run with every status matching what's expected.

**Verified:**
| Area | Result |
|---|---|
| Auth | 401 without a token on all four endpoints |
| Register | 201 with `Location: /api/sensors/{id}` and `Cache-Control: no-store`. The key is 43 chars of base64url and differs per registration. |
| Key storage | The DB holds `sha256(key)`, never the key. GET responses contain neither key nor hash. |
| Validation | 400 for missing, empty, NUL or over-100-character name, and for null or wrong-type `seriesId`. 100 characters gives 201, **counted in code points** (100 emoji passes). 422 for an unknown series. 415, 406. |
| Read | 404 for an unknown or non-numeric id. POST `createdAt` equals GET `createdAt`. |
| Delete | 204, then 404. The key's hash row is gone. A DB-level series delete cascades to sensors. |

**Also changed in this round:**
- Framework JSON error text no longer reaches clients (`AllowInputFormatterExceptionMessages = false`). Wrong-type errors used to expose ``System.Nullable`1[System.Int32]``; they now say "Niepoprawna wartość." and still name the field. This applies to every endpoint.
- `ValidationProblems.Unprocessable(...)` is now shared by the auth and sensor controllers.
- `[MaxCharacters]` replaces `[MaxLength]` on all DTOs (login username and password too), so lengths are counted as JSON Schema and `varchar(n)` count them.

**Code review (subagent). Applied:**
- **POST vs GET `createdAt` mismatch.** .NET ticks are 100 ns, Postgres keeps microseconds. macOS clocks happen to tick in whole microseconds, so it didn't show locally, but on a Linux host the 201 would have had 7 fractional digits and the GET 6. Timestamps are now truncated with `TruncateToMicroseconds()` (reusable for measurements).
- Name length counted UTF-16 units; it now uses `[MaxCharacters]`.
- `Cache-Control: no-store` on the 201, which contains the one-time key.
- Audit log lines on register and unregister (sensor id only, never the key).
- `SensorRegistration` and `GeneratedApiKey` moved to their own files; `SensorRequest` renamed to `RegisterSensorRequest`.

**Not applied:**
- A length limit on `Series.Icon`. The contract sets none, and inventing one could reject contract-valid requests.
- Rejecting whitespace-only names. The contract only requires `minLength: 1`, so they're allowed.

**Dev note:** `dotnet ef …` sometimes fails with "Build failed" while VS Code's C# extension builds the same project in the background. Run `dotnet build` first, then `dotnet ef … --no-build`.

**Next (measurements) will need:**
- An `X-API-Key` authentication handler: hash the presented key, look it up by `ApiKeyHash`, and pin the series from the sensor.
- Updating `Sensor.LastMeasurementAt`.
- `Measurement.SensorId` FK `ON DELETE SET NULL`.
