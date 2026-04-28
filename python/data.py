"""In-memory utility customer fixtures and lookup helpers.

This module backs the bridge endpoints with a static list of customer
records and exposes case- and whitespace-insensitive lookup functions.
"""

CUSTOMERS = [
    {
        "customer_id": "C-10001",
        "first_name": "Ada",
        "last_name": "Lovelace",
        "account_number": "UA-8821-4417",
        "address1": "123 Analytical Way",
        "city": "Springfield",
        "state": "IL",
        "zip": "62704",
    },
    {
        "customer_id": "C-10002",
        "first_name": "Grace",
        "last_name": "Hopper",
        "account_number": "UA-3310-9902",
        "address1": "45 Compiler Ln",
        "city": "Arlington",
        "state": "VA",
        "zip": "22201",
    },
    {
        "customer_id": "C-10003",
        "first_name": "Katherine",
        "last_name": "Johnson",
        "account_number": "UA-7742-0088",
        "address1": "900 Trajectory Rd",
        "city": "Hampton",
        "state": "VA",
        "zip": "23669",
    },
    {
        "customer_id": "C-10004",
        "first_name": "Tester",
        "last_name": "Test",
        "account_number": "UA-1912-0623",
        "address1": "700 5th Ave",
        "city": "Seattle",
        "state": "WA",
        "zip": "98101",
    },
]


def _norm(value):
    """Normalize a string for comparison: collapse whitespace and case-fold."""
    return " ".join(value.split()).casefold()


def find_customer_by_id(customer_id):
    """Return the customer record whose ``customer_id`` matches, or ``None``.

    Comparison is whitespace- and case-insensitive.
    """
    target = _norm(customer_id)
    for customer in CUSTOMERS:
        if _norm(customer["customer_id"]) == target:
            return customer
    return None


def find_customer(
    first_name, last_name, account_number, address1, city, state, zip_code
):
    """Return a customer matching all supplied identity and address fields.

    All fields must match (whitespace- and case-insensitive). When found,
    the returned dict is the source record augmented with convenience
    ``name`` and ``address`` strings. Returns ``None`` if no record matches.
    """
    target = (
        _norm(first_name),
        _norm(last_name),
        _norm(account_number),
        _norm(address1),
        _norm(city),
        _norm(state),
        _norm(zip_code),
    )
    for customer in CUSTOMERS:
        candidate = (
            _norm(customer["first_name"]),
            _norm(customer["last_name"]),
            _norm(customer["account_number"]),
            _norm(customer["address1"]),
            _norm(customer["city"]),
            _norm(customer["state"]),
            _norm(customer["zip"]),
        )
        if candidate == target:
            return {
                **customer,
                "name": f"{customer['first_name']} {customer['last_name']}",
                "address": f"{customer['address1']}, {customer['city']}, {customer['state']} {customer['zip']}",
            }
    return None
