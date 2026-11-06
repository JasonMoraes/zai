#!/usr/bin/env python3
"""Sensor emulator: an interactive console app that sends generated measurements to the API.

It asks for its configuration, then sends each value like a real sensor would:
POST /api/measurements with the sensor key in the X-API-Key header (the key determines the series).

Standard library only, Python 3.9+. Run: python3 sensor_emulator.py
"""
import getpass
import json
import math
import random
import sys
import time
import urllib.error
import urllib.request
from datetime import datetime, timedelta, timezone

TIMEOUT = 90  # free hosting puts the app to sleep; the first request can take about a minute


def log(level, message):
    print(f"{datetime.now():%H:%M:%S} [{level:<5}] {message}")


def ask_text(question, default=None):
    suffix = f" [{default}]" if default else ""
    while True:
        answer = input(f"{question}{suffix}: ").strip()
        if answer or default:
            return answer or default
        print("  To pole jest wymagane.")


def ask_number(question, default, minimum=None, integer=False):
    while True:
        answer = input(f"{question} [{default}]: ").strip() or str(default)
        try:
            number = int(answer) if integer else float(answer)
        except ValueError:
            print("  Podaj liczbę" + (" całkowitą." if integer else "."))
            continue
        if not math.isfinite(number) or (minimum is not None and number < minimum):
            print(f"  Podaj liczbę nie mniejszą niż {minimum}.")
            continue
        return number


def ask_choice(question, options, default):
    """options: a list of (key, description) pairs; returns the key of the chosen option."""
    print(question)
    for number, (_, description) in enumerate(options, start=1):
        print(f"  {number}) {description}")
    while True:
        answer = input(f"Wybór [{default}]: ").strip() or str(default)
        if answer.isdigit() and 1 <= int(answer) <= len(options):
            return options[int(answer) - 1][0]
        print(f"  Wpisz numer od 1 do {len(options)}.")


def ask_configuration():
    print("=" * 60)
    print(" Generator danych: emulator czujnika")
    print(" (Enter przyjmuje wartość domyślną w nawiasie)")
    print("=" * 60)
    config = {}
    while True:
        config["url"] = ask_text("Adres API", "http://localhost:5027").rstrip("/")
        if config["url"].startswith(("http://", "https://")):
            break
        print("  Adres musi zaczynać się od http:// albo https://.")
    while True:
        config["key"] = getpass.getpass("Klucz API czujnika (niewidoczny podczas wpisywania): ").strip()
        if config["key"]:
            break
        print("  Klucz jest wymagany.")
    config["past"] = ask_choice("Tryb pracy:", [
        (False, "bieżące wyniki w stałych odstępach czasu"),
        (True, "seria wyników ze znacznikami czasu z przeszłości (dane przykładowe)"),
    ], 1)
    if config["past"]:
        config["count"] = ask_number("Liczba wyników", 20, minimum=1, integer=True)
        config["interval"] = ask_number("Odstęp między znacznikami czasu (sekundy)", 3600, minimum=1)
    else:
        config["count"] = ask_number("Liczba wyników (0 = bez końca)", 0, minimum=0, integer=True)
        config["interval"] = ask_number("Odstęp między wynikami (sekundy)", 5, minimum=1)
    config["mode"] = ask_choice("Sposób generowania wartości:", [
        ("random", "losowo z zakresu"),
        ("sine", "sinusoida o okresie 1 doby"),
    ], 1)
    while True:
        config["min"] = ask_number("Najmniejsza wartość", 0)
        config["max"] = ask_number("Największa wartość", 100)
        if config["min"] < config["max"]:
            break
        print("  Najmniejsza wartość musi być mniejsza niż największa.")
    return config


def confirm(config):
    count = "bez końca" if config["count"] == 0 else config["count"]
    print("-" * 60)
    print(f" Adres API:      {config['url']}")
    print(f" Klucz:          …{config['key'][-4:]}")
    print(f" Tryb:           {'z przeszłości' if config['past'] else 'bieżący'}")
    print(f" Liczba wyników: {count}, co {config['interval']:g} s")
    print(f" Wartości:       {'losowe' if config['mode'] == 'random' else 'sinusoida'} z zakresu "
          f"[{config['min']:g}, {config['max']:g}]")
    print("-" * 60)
    return input("Rozpocząć? [T/n]: ").strip().lower() in ("", "t", "tak", "y", "yes")


def generate_value(config, moment):
    low, high = config["min"], config["max"]
    if config["mode"] == "random":
        return round(random.uniform(low, high), 2)
    middle, amplitude = (low + high) / 2, (high - low) / 2
    phase = 2 * math.pi * (moment.timestamp() % 86400) / 86400
    value = middle + amplitude * math.sin(phase) + random.gauss(0, amplitude * 0.05)
    return round(min(high, max(low, value)), 2)


def call_api(method, url, body=None, key=None):
    """Sends one HTTP request; returns (status, body as dict, time in ms). Status 0 means no connection."""
    headers = {"Accept": "application/json"}
    data = None
    if body is not None:
        data = json.dumps(body).encode()
        headers["Content-Type"] = "application/json"
    if key:
        headers["X-API-Key"] = key
    request = urllib.request.Request(url, data=data, method=method, headers=headers)
    started = time.monotonic()
    try:
        with urllib.request.urlopen(request, timeout=TIMEOUT) as response:
            status, raw = response.status, response.read()
    except urllib.error.HTTPError as error:
        status, raw = error.code, error.read()
    except (urllib.error.URLError, OSError) as error:
        return 0, {"detail": str(getattr(error, "reason", error))}, 0
    elapsed = (time.monotonic() - started) * 1000
    try:
        return status, json.loads(raw or b"{}"), elapsed
    except ValueError:
        return status, {}, elapsed


def describe_rejection(status, body):
    if status == 401:
        return "nieprawidłowy klucz API (czujnik nie istnieje albo został wyrejestrowany)"
    details = [f"{e.get('field')}: {e.get('message')}" for e in body.get("errors", [])]
    return "; ".join(details) or body.get("detail") or body.get("title") or "nieznany błąd"


def check_api(url):
    log("INFO", f"Sprawdzanie dostępności API: GET {url}/api/health")
    log("INFO", "Jeśli aplikacja była uśpiona (darmowy hosting), może to potrwać około minuty…")
    status, body, ms = call_api("GET", url + "/api/health")
    if status == 200:
        log("INFO", f"API działa (HTTP 200, {ms:.0f} ms)")
        return True
    if status == 0:
        log("BŁĄD", f"Brak połączenia z API: {body.get('detail')}")
    else:
        log("BŁĄD", f"API odpowiedziało HTTP {status}; sprawdź adres")
    return False


def run(config):
    log("INFO", "Uruchamianie generatora")
    if not check_api(config["url"]):
        return 2

    accepted = rejected = 0
    started_at = datetime.now(timezone.utc)
    sent = 0
    log("INFO", "Rozpoczęcie wysyłania wyników" + ("" if config["past"] or config["count"] else " (Ctrl+C kończy)"))
    try:
        while config["count"] == 0 or sent < config["count"]:
            if config["past"]:
                # Spaced `interval` apart so that the last result falls on the moment the run started.
                moment = started_at - timedelta(seconds=config["interval"] * (config["count"] - 1 - sent))
            else:
                moment = datetime.now(timezone.utc)
            value = generate_value(config, moment)
            timestamp = moment.strftime("%Y-%m-%dT%H:%M:%SZ")
            sent += 1

            log("INFO", f"[{sent}] Wysyłanie: POST /api/measurements  wartość={value}  czas={timestamp}")
            status, body, ms = call_api("POST", config["url"] + "/api/measurements",
                                        {"value": value, "timestamp": timestamp}, config["key"])
            if status == 201:
                accepted += 1
                log("INFO", f"[{sent}] Przyjęto: HTTP 201, id wyniku {body.get('id')} ({ms:.0f} ms)")
            elif status == 0:
                log("BŁĄD", f"[{sent}] Brak połączenia z API: {body.get('detail')}; przerywam")
                break
            else:
                rejected += 1
                log("UWAGA", f"[{sent}] ODRZUCONO: HTTP {status}, {describe_rejection(status, body)}")

            if not config["past"] and (config["count"] == 0 or sent < config["count"]):
                time.sleep(config["interval"])
    except KeyboardInterrupt:
        print()
        log("INFO", "Przerwano przez użytkownika (Ctrl+C)")

    log("INFO", f"Zakończono. Wysłano: {sent}, przyjęto: {accepted}, odrzucono: {rejected}")
    return 1 if rejected else 0


def main():
    try:
        config = ask_configuration()
        if not confirm(config):
            print("Anulowano.")
            return 0
    except (KeyboardInterrupt, EOFError):
        print("\nAnulowano.")
        return 0
    return run(config)


if __name__ == "__main__":
    sys.exit(main())
