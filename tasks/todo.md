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
