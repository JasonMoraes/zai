# Generator danych (emulator czujnika): instrukcja użytkownika

Generator udaje autonomiczny czujnik IoT: generuje wartości pomiarów i wysyła je do aplikacji
**przez API**, dokładnie tak, jak robiłby to prawdziwy czujnik:

```
POST /api/measurements
X-API-Key: <klucz czujnika>
Content-Type: application/json

{ "value": 21.75, "timestamp": "2026-11-06T11:46:48Z" }
```

Seria, do której trafia wynik, wynika z klucza czujnika. Generator nigdy nie zapisuje niczego bezpośrednio do bazy
danych.

Spis treści
1. Wymagania
2. Zanim zaczniesz: seria, czujnik i klucz API
3. Uruchomienie i przykładowa sesja
4. Pytania konfiguracyjne
5. Tryby pracy i sposoby generowania wartości
6. Typowe scenariusze
7. Dziennik: co oznacza każdy komunikat
8. Kody wyjścia
9. Rozwiązywanie problemów
10. Bezpieczeństwo klucza API

---

## 1. Wymagania

- **Python 3.9 lub nowszy.** Sprawdzenie: `python3 --version` (na Windows: `py --version`).
- **Żadnych dodatkowych bibliotek.** Generator korzysta wyłącznie z biblioteki standardowej Pythona, więc nie trzeba
  nic instalować (`pip` nie jest potrzebny).
- Dostęp do działającej aplikacji: lokalnie (`http://localhost:5027`) albo wdrożonej (np. `https://twoja-aplikacja.onrender.com`).

## 2. Zanim zaczniesz: seria, czujnik i klucz API

Każdy wynik musi należeć do serii, a generator musi przedstawić się kluczem API zarejestrowanego czujnika.

1. **Seria.** Administrator tworzy serię z nazwą i dopuszczalnym przedziałem wartości, np. „Temperatura”, od −30 do 45.
2. **Czujnik.** Administrator rejestruje czujnik w tej serii. W odpowiedzi aplikacja **jednorazowo** pokazuje klucz API
   (43 znaki). Skopiuj go od razu: później nie da się go odczytać, bo serwer przechowuje tylko jego skrót (SHA-256).

Czujnik można zarejestrować przez API (lokalnie także przez Swagger UI pod `http://localhost:5027/api/swagger`):

```bash
API=http://localhost:5027      # albo adres wdrożonej aplikacji

TOKEN=$(curl -s -X POST $API/api/auth/login -H 'Content-Type: application/json' \
  -d '{"username":"admin","password":"<hasło>"}' | python3 -c 'import sys,json; print(json.load(sys.stdin)["accessToken"])')

curl -s $API/api/series        # lista serii: znajdź id właściwej serii

curl -s -X POST $API/api/sensors -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"name":"Czujnik balkon","seriesId":1}'
```

Ostatnie polecenie zwraca m.in. `"apiKey": "…"`: to klucz, który podasz generatorowi.

> **Klucz zgubiony?** Nie da się go odzyskać. Zarejestruj nowy czujnik w tej samej serii, a stary wyrejestruj
> (`DELETE /api/sensors/{id}`). Wyniki zebrane przez stary czujnik pozostają w serii.

## 3. Uruchomienie i przykładowa sesja

```bash
cd generator
python3 sensor_emulator.py
```

Program zadaje kilka pytań. **Enter przyjmuje wartość domyślną** podaną w nawiasie kwadratowym. Klucz API jest
wpisywany niewidocznie, jak hasło. Poniżej rzeczywisty przebieg (5 wyników z przeszłości, sinusoida od −5 do 15):

```
============================================================
 Generator danych: emulator czujnika
 (Enter przyjmuje wartość domyślną w nawiasie)
============================================================
Adres API [http://localhost:5027]:
Klucz API czujnika (niewidoczny podczas wpisywania):
Tryb pracy:
  1) bieżące wyniki w stałych odstępach czasu
  2) seria wyników ze znacznikami czasu z przeszłości (dane przykładowe)
Wybór [1]: 2
Liczba wyników [20]: 5
Odstęp między znacznikami czasu (sekundy) [3600]:
Sposób generowania wartości:
  1) losowo z zakresu
  2) sinusoida o okresie 1 doby
Wybór [1]: 2
Najmniejsza wartość [0]: -5
Największa wartość [100]: 15
------------------------------------------------------------
 Adres API:      http://localhost:5027
 Klucz:          …nWsA
 Tryb:           z przeszłości
 Liczba wyników: 5, co 3600 s
 Wartości:       sinusoida z zakresu [-5, 15]
------------------------------------------------------------
Rozpocząć? [T/n]:
12:46:48 [INFO ] Uruchamianie generatora
12:46:48 [INFO ] Sprawdzanie dostępności API: GET http://localhost:5027/api/health
12:46:48 [INFO ] Jeśli aplikacja była uśpiona (darmowy hosting), może to potrwać około minuty…
12:46:48 [INFO ] API działa (HTTP 200, 14 ms)
12:46:48 [INFO ] Rozpoczęcie wysyłania wyników
12:46:48 [INFO ] [1] Wysyłanie: POST /api/measurements  wartość=14.8  czas=2026-11-06T07:46:48Z
12:46:48 [INFO ] [1] Przyjęto: HTTP 201, id wyniku 953 (69 ms)
12:46:48 [INFO ] [2] Wysyłanie: POST /api/measurements  wartość=12.05  czas=2026-11-06T08:46:48Z
12:46:48 [INFO ] [2] Przyjęto: HTTP 201, id wyniku 954 (9 ms)
…
12:46:48 [INFO ] [5] Wysyłanie: POST /api/measurements  wartość=4.98  czas=2026-11-06T11:46:48Z
12:46:48 [INFO ] [5] Przyjęto: HTTP 201, id wyniku 957 (4 ms)
12:46:48 [INFO ] Zakończono. Wysłano: 5, przyjęto: 5, odrzucono: 0
```

Przed startem program pokazuje podsumowanie. Odpowiedź `n` anuluje; Enter, `t` albo `tak` uruchamia wysyłanie.
Ctrl+C podczas zadawania pytań przerywa bez wysyłania czegokolwiek („Anulowano.”).

## 4. Pytania konfiguracyjne

| Pytanie | Domyślnie | Dozwolone wartości | Uwagi |
|---|---|---|---|
| Adres API | `http://localhost:5027` | adres z `http://` albo `https://` | Adres samej aplikacji, bez `/api` na końcu, np. `https://twoja-aplikacja.onrender.com`. |
| Klucz API czujnika | brak (wymagany) | klucz z rejestracji czujnika | Wpisywany niewidocznie. Spacje na początku i końcu są usuwane. |
| Tryb pracy | `1` (bieżący) | `1` lub `2` | Patrz rozdział 5. |
| Liczba wyników | `0` w trybie bieżącym, `20` w trybie z przeszłości | liczba całkowita ≥ 0 (z przeszłości: ≥ 1) | `0` w trybie bieżącym = bez końca, aż do Ctrl+C. |
| Odstęp (sekundy) | `5` w trybie bieżącym, `3600` w trybie z przeszłości | liczba ≥ 1 (może być ułamkowa) | Bieżący: przerwa między wysłaniami. Z przeszłości: odstęp między znacznikami czasu. |
| Sposób generowania | `1` (losowo) | `1` lub `2` | Patrz rozdział 5. |
| Najmniejsza / największa wartość | `0` / `100` | liczby, najmniejsza < największej | Ujemne wartości wpisuje się normalnie, np. `-5`. |

Błędna odpowiedź (np. litery zamiast liczby, numer spoza listy, minimum ≥ maksimum) nie przerywa programu: pojawia
się krótka podpowiedź i to samo pytanie zostaje zadane ponownie.

## 5. Tryby pracy i sposoby generowania wartości

**Tryby pracy**

| Tryb | Działanie | Do czego |
|---|---|---|
| **1. Bieżące wyniki** | Wysyła wynik z aktualnym czasem, czeka zadany odstęp, wysyła kolejny… aż do osiągnięcia liczby wyników albo Ctrl+C. | Pokaz „na żywo”: wyniki pojawiają się na wykresie na bieżąco. |
| **2. Z przeszłości** | Od razu wysyła całą serię. Znaczniki czasu są cofnięte co zadany odstęp, a **ostatni wynik ma czas uruchomienia**. Np. 48 wyników co 3600 s = ostatnie 48 godzin. | Szybkie wypełnienie serii historią, np. danymi przykładowymi. |

**Sposoby generowania**

| Sposób | Wartości |
|---|---|
| **1. Losowo** | Niezależne losowe wartości z przedziału [najmniejsza, największa], zaokrąglone do 2 miejsc po przecinku. |
| **2. Sinusoida** | Łagodna krzywa o okresie 1 doby z niewielkim szumem (5% amplitudy), nie wychodzi poza zadany przedział. Zależy od pory dnia (UTC): maksimum około 06:00 UTC, minimum około 18:00 UTC. Dobrze wygląda na wykresie. |

## 6. Typowe scenariusze

### A. Dane przykładowe dla kilku serii

1. Utwórz serie, np. Temperatura (−30…45 °C), Wilgotność (0…100 %), Ciśnienie (950…1060 hPa).
2. W każdej zarejestruj po jednym czujniku i zapisz klucze.
3. Dla każdego klucza uruchom generator: tryb **2**, np. **48** wyników co **3600** s, sinusoida, zakres wyraźnie
   wewnątrz przedziału serii (np. Temperatura −2…12, Wilgotność 55…95, Ciśnienie 995…1030).
4. Sprawdź wynik: `GET /api/measurements?series=<id>` powinno zwrócić tyle wyników, ile wysłano (tu 48).

### B. Pokaz na żywo

Tryb **1**, liczba wyników **0**, odstęp np. **5** s. Wyniki pojawiają się w aplikacji co 5 sekund. Ctrl+C kończy
i wypisuje podsumowanie.

### C. Odrzucenie wyniku spoza przedziału serii

Podaj zakres generowania wykraczający poza przedział serii. Np. dla Temperatury (−30…45) najmniejsza 30, największa
60. Serwer odrzuca wartości powyżej 45 kodem 422, a generator pokazuje powód:

```
12:46:48 [INFO ] [1] Wysyłanie: POST /api/measurements  wartość=52.73  czas=2026-11-06T11:46:48Z
12:46:48 [UWAGA] [1] ODRZUCONO: HTTP 422, value: Wartość musi należeć do przedziału serii [-30, 45].
12:46:52 [INFO ] [3] Wysyłanie: POST /api/measurements  wartość=43.71  czas=2026-11-06T11:46:52Z
12:46:52 [INFO ] [3] Przyjęto: HTTP 201, id wyniku 958 (16 ms)
12:46:52 [INFO ] Zakończono. Wysłano: 3, przyjęto: 1, odrzucono: 2
```

Odrzucenie jest również zapisywane w dzienniku serwera.

### D. Wyrejestrowany czujnik

Po wyrejestrowaniu czujnika jego klucz natychmiast przestaje działać:

```
12:46:52 [UWAGA] [1] ODRZUCONO: HTTP 401, nieprawidłowy klucz API (czujnik nie istnieje albo został wyrejestrowany)
```

### E. Aplikacja na darmowym hostingu (uśpiona)

Darmowy plan Render usypia aplikację po 15 minutach bez ruchu. Pierwsze żądanie budzi ją i może trwać **około minuty**.
Generator czeka na odpowiedź do 90 sekund, więc wystarczy cierpliwość przy kroku „Sprawdzanie dostępności API”.

## 7. Dziennik: co oznacza każdy komunikat

Każdy wpis ma format `GG:MM:SS [POZIOM] treść`, gdzie godzina jest lokalna. Znaczniki czasu wysyłane do API są w UTC
(końcówka `Z`).

| Komunikat | Znaczenie |
|---|---|
| `[INFO ] Uruchamianie generatora` | Konfiguracja przyjęta, start. |
| `[INFO ] Sprawdzanie dostępności API: GET …/api/health` | Sprawdzenie, czy aplikacja odpowiada (i ewentualne obudzenie jej). |
| `[INFO ] API działa (HTTP 200, 14 ms)` | Aplikacja działa; w nawiasie czas odpowiedzi. |
| `[INFO ] [n] Wysyłanie: POST /api/measurements  wartość=…  czas=…` | Wysyłany jest n-ty wynik: wartość i znacznik czasu. |
| `[INFO ] [n] Przyjęto: HTTP 201, id wyniku 953 (9 ms)` | Wynik zapisany; id nadane przez serwer i czas odpowiedzi. |
| `[UWAGA] [n] ODRZUCONO: HTTP 422, value: …` | Wynik niezgodny z regułami serii (np. spoza przedziału). Powód podaje serwer. |
| `[UWAGA] [n] ODRZUCONO: HTTP 422, timestamp: …` | Znacznik czasu późniejszy niż 5 minut od czasu serwera (np. źle ustawiony zegar). |
| `[UWAGA] [n] ODRZUCONO: HTTP 401, nieprawidłowy klucz API …` | Klucz błędny albo czujnik wyrejestrowany. |
| `[BŁĄD ] Brak połączenia z API: …` | Nie udało się połączyć (zły adres, brak sieci, aplikacja wyłączona). |
| `[BŁĄD ] API odpowiedziało HTTP …; sprawdź adres` | Pod podanym adresem odpowiada coś innego niż aplikacja (zły adres). |
| `[BŁĄD ] [n] Brak połączenia z API: …; przerywam` | Połączenie zerwane w trakcie wysyłania; dalsze wyniki nie są wysyłane. |
| `[INFO ] Przerwano przez użytkownika (Ctrl+C)` | Zatrzymane ręcznie. |
| `[INFO ] Zakończono. Wysłano: …, przyjęto: …, odrzucono: …` | Podsumowanie. |

## 8. Kody wyjścia

Przydatne, gdy generator jest uruchamiany ze skryptu:

| Kod | Kiedy |
|---|---|
| `0` | Wszystkie wysłane wyniki przyjęte, albo konfiguracja anulowana. |
| `1` | Co najmniej jeden wynik odrzucony przez API (422, 401). |
| `2` | API nieosiągalne już na starcie (sprawdzenie `/api/health` nie powiodło się). |

Zerwanie połączenia w trakcie wysyłania kończy pracę z kodem `0` albo `1`, zależnie od tego, czy wcześniej coś
odrzucono. Szczegóły zawsze są w dzienniku.

## 9. Rozwiązywanie problemów

| Objaw | Przyczyna | Rozwiązanie |
|---|---|---|
| `Brak połączenia z API: … Connection refused` | Aplikacja nie działa pod tym adresem albo zły port. | Lokalnie uruchom API (`dotnet run --project Api` w `backend/`). Sprawdź adres. |
| `Brak połączenia … nodename nor servname` / `Name or service not known` | Literówka w nazwie hosta albo brak internetu. | Popraw adres, sprawdź sieć. |
| Długie oczekiwanie przy „Sprawdzanie dostępności API” | Aplikacja na darmowym hostingu była uśpiona. | Poczekaj do minuty. Przy powtórnym uruchomieniu będzie szybko. |
| `API odpowiedziało HTTP 404` | Adres wskazuje na coś innego niż aplikację, np. dopisane `/api`. | Podaj sam adres aplikacji, bez `/api`. |
| `ODRZUCONO: HTTP 401` | Zły klucz (literówka, ucięty przy kopiowaniu) albo wyrejestrowany czujnik. | Wklej klucz ponownie; jeśli zginął, zarejestruj nowy czujnik (rozdział 2). |
| `ODRZUCONO: HTTP 422, value: …` | Zakres generowania wychodzi poza przedział serii. | Ustaw najmniejszą/największą wartość wewnątrz przedziału serii (albo zostaw, jeśli to pokaz odrzucenia). |
| `ODRZUCONO: HTTP 422, timestamp: …` | Zegar komputera spieszy się o ponad 5 minut. | Ustaw poprawną datę i godzinę w systemie. |
| `python3: command not found` | Brak Pythona albo inna nazwa polecenia. | Zainstaluj Python 3.9+; na Windows użyj `py sensor_emulator.py`. |

## 10. Bezpieczeństwo klucza API

- Klucza **nie ma w kodzie** ani w plikach konfiguracyjnych. Podaje się go przy każdym uruchomieniu, a wpisywanie jest
  niewidoczne. Podsumowanie pokazuje tylko 4 ostatnie znaki.
- Klucz daje prawo zapisu wyników do jednej serii. Nie publikuj go (repozytorium, czat, zrzuty ekranu).
- Jeśli klucz wyciekł, wyrejestruj czujnik: klucz natychmiast przestaje działać, a zebrane wyniki zostają.
