#!/usr/bin/env python3
"""Generator danych (emulator czujnika), F13.

Generuje wartości i wysyła je do API tak, jak robiłby to czujnik:
POST /api/measurements z kluczem czujnika w nagłówku X-API-Key (seria wynika z klucza).

  --past   wysyła --count wyników od razu, ze znacznikami czasu z przeszłości (dane przykładowe);
  bez --past  wysyła bieżący wynik co --interval sekund (Ctrl+C kończy).

Tylko biblioteka standardowa, Python 3.9+.
"""
import argparse
import json
import math
import os
import random
import sys
import time
import urllib.error
import urllib.request
from datetime import datetime, timedelta, timezone


def generate_value(mode, moment, low, high):
    """'random': losowa wartość z [low, high]; 'sine': sinusoida o okresie 1 doby z małym szumem."""
    if mode == "random":
        return round(random.uniform(low, high), 2)
    middle, amplitude = (low + high) / 2, (high - low) / 2
    phase = 2 * math.pi * (moment.timestamp() % 86400) / 86400
    value = middle + amplitude * math.sin(phase) + random.gauss(0, amplitude * 0.05)
    return round(min(high, max(low, value)), 2)


def send(url, key, value, moment):
    """Wysyła jeden wynik; zwraca (kod HTTP, treść odpowiedzi jako dict)."""
    body = json.dumps({"value": value, "timestamp": moment.strftime("%Y-%m-%dT%H:%M:%SZ")}).encode()
    request = urllib.request.Request(
        url.rstrip("/") + "/api/measurements",
        data=body,
        method="POST",
        headers={"Content-Type": "application/json", "X-API-Key": key},
    )
    try:
        # Długi limit czasu: darmowy hosting usypia aplikację i pierwsze żądanie może trwać około minuty.
        with urllib.request.urlopen(request, timeout=90) as response:
            return response.status, json.loads(response.read() or "{}")
    except urllib.error.HTTPError as error:
        try:
            return error.code, json.loads(error.read() or "{}")
        except ValueError:
            return error.code, {}


def describe_rejection(status, body):
    """Czytelny komunikat o odrzuceniu na podstawie odpowiedzi API (problem+json)."""
    if status == 401:
        return "nieprawidłowy klucz API (czujnik nie istnieje albo został wyrejestrowany)"
    details = [f"{e.get('field')}: {e.get('message')}" for e in body.get("errors", [])]
    return "; ".join(details) or body.get("detail") or body.get("title") or "nieznany błąd"


def main():
    parser = argparse.ArgumentParser(description="Emulator czujnika: wysyła wyniki pomiarów do API.")
    parser.add_argument("--url", default=os.environ.get("ZAI_API_URL"),
                        help="adres API, np. https://zai-api.onrender.com (lub zmienna ZAI_API_URL)")
    parser.add_argument("--key", default=os.environ.get("ZAI_API_KEY"),
                        help="klucz API czujnika (lub zmienna ZAI_API_KEY)")
    parser.add_argument("--count", type=int, default=10, help="liczba wyników (0 = bez końca, tylko bez --past)")
    parser.add_argument("--interval", type=float, default=5, help="odstęp między wynikami w sekundach")
    parser.add_argument("--mode", choices=["random", "sine"], default="random", help="sposób generowania wartości")
    parser.add_argument("--min", type=float, default=0, help="najmniejsza generowana wartość (ujemną podaj jako --min=-5)")
    parser.add_argument("--max", type=float, default=100, help="największa generowana wartość")
    parser.add_argument("--past", action="store_true",
                        help="wyślij od razu --count wyników ze znacznikami czasu z przeszłości")
    args = parser.parse_args()

    if not args.url or not args.key:
        parser.error("podaj adres API (--url / ZAI_API_URL) i klucz czujnika (--key / ZAI_API_KEY)")
    if args.min >= args.max or args.interval <= 0 or args.count < 0 or (args.past and args.count == 0):
        parser.error("wymagane: --min < --max, --interval > 0, --count >= 0 (z --past co najmniej 1)")

    accepted = rejected = 0
    now = datetime.now(timezone.utc)
    i = 0
    try:
        while args.count == 0 or i < args.count:
            if args.past:
                # Rozłożone co --interval, tak żeby ostatni wynik przypadł na chwilę obecną.
                moment = now - timedelta(seconds=args.interval * (args.count - 1 - i))
            else:
                moment = datetime.now(timezone.utc)
            value = generate_value(args.mode, moment, args.min, args.max)

            try:
                status, body = send(args.url, args.key.strip(), value, moment)
            except (urllib.error.URLError, OSError) as error:
                print(f"BŁĄD: brak połączenia z API ({error})")
                return 2

            stamp = moment.strftime("%Y-%m-%d %H:%M:%S UTC")
            if status == 201:
                accepted += 1
                print(f"[{i + 1}] {stamp}  {value:>10}  -> przyjęto")
            else:
                rejected += 1
                print(f"[{i + 1}] {stamp}  {value:>10}  -> ODRZUCONO ({status}): {describe_rejection(status, body)}")

            i += 1
            if not args.past and (args.count == 0 or i < args.count):
                time.sleep(args.interval)
    except KeyboardInterrupt:
        print("\nPrzerwano.")

    print(f"\nPrzyjęto: {accepted}, odrzucono: {rejected}")
    return 1 if rejected else 0


if __name__ == "__main__":
    sys.exit(main())
