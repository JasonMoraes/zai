# Serie pomiarowe

Aplikacja do zbierania i prezentacji wyników pomiarów z czujników IoT, pogrupowanych w serie.

## Struktura

- `backend/`: REST API (ASP.NET Core 8, EF Core, PostgreSQL)
- `generator/`: emulator czujnika wysyłający wyniki do API ([instrukcja](generator/README.md))
- `render.yaml`: konfiguracja wdrożenia na Render

## Uruchomienie lokalne

Wymagane: .NET 8 SDK, PostgreSQL (baza `zai`, użytkownik `zai`, hasło `zai`).

```bash
cd backend/Api
dotnet run
```

API działa pod `http://localhost:5027`, a Swagger UI pod `http://localhost:5027/api/swagger`.
Migracje bazy i konto administratora tworzą się przy starcie (dane logowania w `backend/Api/appsettings.Development.json`).

Generator:

```bash
python3 generator/sensor_emulator.py
```

## Wdrożenie

Render (darmowy plan): po 15 minutach bez ruchu aplikacja jest usypiana, a pierwsze żądanie po przerwie trwa około minuty.

Aplikacja wdrożona jest na darmowym planie baz danych, toteż w dniu 4 grudnia 2026 maszyna zostanie zatrzymana i będzie wymagana rekonfiguracja. W przypadku sprawdzania repozytorium po tej dacie konieczne będzie skontaktowanie się z autorem repozytorium.


## Wykorzystanie agentów AI i LLM

W czasie tworzenia kodu wykorzystane było narzędzie Claude Code (model Opus 5.5) do tworzenia fragmentów kodu i pliku konfiguracyjnego Render.