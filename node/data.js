/**
 * @file In-memory utility customer fixtures and lookup helpers.
 *
 * This module backs the bridge endpoints with a static list of customer
 * records and exposes case- and whitespace-insensitive lookup functions.
 */

"use strict";

const CUSTOMERS = [
  {
    customer_id: "C-10001",
    first_name: "Ada",
    last_name: "Lovelace",
    account_number: "UA-8821-4417",
    address1: "123 Analytical Way",
    city: "Springfield",
    state: "IL",
    zip: "62704",
  },
  {
    customer_id: "C-10002",
    first_name: "Grace",
    last_name: "Hopper",
    account_number: "UA-3310-9902",
    address1: "45 Compiler Ln",
    city: "Arlington",
    state: "VA",
    zip: "22201",
  },
  {
    customer_id: "C-10003",
    first_name: "Katherine",
    last_name: "Johnson",
    account_number: "UA-7742-0088",
    address1: "900 Trajectory Rd",
    city: "Hampton",
    state: "VA",
    zip: "23669",
  },
  {
    customer_id: "C-10004",
    first_name: "Tester",
    last_name: "Test",
    account_number: "UA-1912-0623",
    address1: "700 5th Ave",
    city: "Seattle",
    state: "WA",
    zip: "98101",
  },
];

/** Normalize a string for comparison: collapse whitespace and case-fold. */
function norm(value) {
  return String(value).split(/\s+/).filter(Boolean).join(" ").toLowerCase();
}

/**
 * Return the customer record whose `customer_id` matches, or `null`.
 *
 * Comparison is whitespace- and case-insensitive.
 */
function findCustomerById(customerId) {
  const target = norm(customerId);
  return CUSTOMERS.find((c) => norm(c.customer_id) === target) || null;
}

/**
 * Return a customer matching all supplied identity and address fields.
 *
 * All fields must match (whitespace- and case-insensitive). When found,
 * the returned object is the source record augmented with convenience
 * `name` and `address` strings. Returns `null` if no record matches.
 */
function findCustomer(
  firstName,
  lastName,
  accountNumber,
  address1,
  city,
  state,
  zipCode,
) {
  const target = [
    norm(firstName),
    norm(lastName),
    norm(accountNumber),
    norm(address1),
    norm(city),
    norm(state),
    norm(zipCode),
  ];
  for (const customer of CUSTOMERS) {
    const candidate = [
      norm(customer.first_name),
      norm(customer.last_name),
      norm(customer.account_number),
      norm(customer.address1),
      norm(customer.city),
      norm(customer.state),
      norm(customer.zip),
    ];
    if (candidate.every((v, i) => v === target[i])) {
      return {
        ...customer,
        name: `${customer.first_name} ${customer.last_name}`,
        address: `${customer.address1}, ${customer.city}, ${customer.state} ${customer.zip}`,
      };
    }
  }
  return null;
}

module.exports = { CUSTOMERS, findCustomer, findCustomerById };
