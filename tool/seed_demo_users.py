"""Creates demo users that are visible around a location, so testers see someone nearby.

Only the Python standard library is needed. Run it again to refresh their locations (the server
hides users whose location is older than 24 hours). Examples:

    python tool/seed_demo_users.py                                  # local server, Belgrade
    python tool/seed_demo_users.py --server http://188.245.72.17:4050 --lat 45.2671 --lon 19.8335
    python tool/seed_demo_users.py --delete                         # remove the demo users

On an Android emulator, move the device to the same place:
    adb emu geo fix <lon> <lat>
The iOS Simulator has Features > Location > Custom Location.
"""

import argparse
import json
import math
import urllib.error
import urllib.request

PASSWORD = "demo-password"

# (username, private profile, business profile or None, offset north m, offset east m)
USERS = [
    (
        "demo.marko",
        {
            "firstName": "Marko",
            "lastName": "Marković",
            "email": "marko@example.com",
            "phoneNumber": "+381 60 111 2233",
            "description": "Coffee lover, looking for a running partner.",
            "twitterUrl": "@marko",
        },
        None,
        40,
        25,
    ),
    (
        "demo.jelena",
        {
            "firstName": "Jelena",
            "lastName": "Jovanović",
            "email": "jelena@example.com",
            "description": "UX designer. Ask me about typography.",
            "linkedinUrl": "linkedin.com/in/jelena",
            "dateOfBirth": "1994-03-12",
        },
        {
            "companyName": "Studio Pixel",
            "email": "hello@studiopixel.example.com",
            "website": "studiopixel.example.com",
            "phoneNumber": "011 222 333",
            "address": "Knez Mihailova 10, Beograd",
            "description": "Design studio: brands, apps and websites.",
        },
        -60,
        80,
    ),
    (
        "demo.stefan",
        {
            "firstName": "Stefan",
            "lastName": "Petrović",
            "phoneNumber": "+381 64 555 0101",
            "description": "Guitarist. Playing at the square tonight!",
            "facebookUrl": "stefan.petrovic",
        },
        None,
        150,
        -120,
    ),
    (
        "demo.kafeterija",
        {"firstName": "Milica", "lastName": "Ilić", "email": "milica@example.com"},
        {
            "companyName": "Kafeterija",
            "email": "info@kafeterija.example.com",
            "website": "kafeterija.example.com",
            "address": "Kralja Petra 5, Beograd",
            "description": "Specialty coffee, roasted in house.",
        },
        -200,
        -300,
    ),
]


def request(server, method, path, token=None, body=None):
    data = None if body is None else json.dumps(body).encode()
    req = urllib.request.Request(server + path, data=data, method=method)
    req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", f"Bearer {token}")
    try:
        with urllib.request.urlopen(req, timeout=10) as response:
            raw = response.read()
            return json.loads(raw) if raw else None
    except urllib.error.HTTPError as error:
        raise RuntimeError(f"{method} {path}: {error.code} {error.read().decode()}") from None


def login_or_register(server, username):
    try:
        return request(server, "POST", "/api/v1/auth/login", body={"username": username, "password": PASSWORD})
    except RuntimeError:
        return request(server, "POST", "/api/v1/auth/register", body={"username": username, "password": PASSWORD})


def offset(lat, lon, north_m, east_m):
    return lat + north_m / 111_320, lon + east_m / (111_320 * math.cos(math.radians(lat)))


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--server", default="http://localhost:4050")
    parser.add_argument("--lat", type=float, default=44.8176, help="default: Knez Mihailova, Belgrade")
    parser.add_argument("--lon", type=float, default=20.4569)
    parser.add_argument("--delete", action="store_true", help="delete the demo users instead")
    args = parser.parse_args()
    server = args.server.rstrip("/")

    for username, private, business, north, east in USERS:
        token = login_or_register(server, username)["accessToken"]
        if args.delete:
            request(server, "DELETE", "/api/v1/me", token)
            print(f"deleted {username}")
            continue
        request(server, "PATCH", "/api/v1/me/private-profile", token, private)
        lat, lon = offset(args.lat, args.lon, north, east)
        request(server, "PUT", "/api/v1/me/visibility/private", token, {"latitude": lat, "longitude": lon})
        if business:
            request(server, "PATCH", "/api/v1/me/business-profile", token, business)
            request(server, "PUT", "/api/v1/me/visibility/business", token, {"latitude": lat, "longitude": lon})
        modes = "private + business" if business else "private"
        print(f"{username:18} visible ({modes}) at {lat:.5f}, {lon:.5f}")

    if not args.delete:
        print(f"\nPassword of every demo user: {PASSWORD}")
        print(f"Android emulator: adb emu geo fix {args.lon} {args.lat}")


if __name__ == "__main__":
    main()
