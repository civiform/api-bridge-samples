// <summary>
// In-memory utility customer fixtures and lookup helpers.
//
// This module backs the bridge endpoints with a static list of customer
// records and exposes case- and whitespace-insensitive lookup functions.
// </summary>

namespace Bridge;

public record Customer(
    string CustomerId,
    string FirstName,
    string LastName,
    string AccountNumber,
    string Address1,
    string City,
    string State,
    string Zip);

public record CustomerMatch(
    Customer Source,
    string Name,
    string Address);

public static class Data
{
    public static readonly IReadOnlyList<Customer> Customers = new List<Customer>
    {
        new("C-10001", "Ada", "Lovelace", "UA-8821-4417", "123 Analytical Way", "Springfield", "IL", "62704"),
        new("C-10002", "Grace", "Hopper", "UA-3310-9902", "45 Compiler Ln", "Arlington", "VA", "22201"),
        new("C-10003", "Katherine", "Johnson", "UA-7742-0088", "900 Trajectory Rd", "Hampton", "VA", "23669"),
        new("C-10004", "Tester", "Test", "UA-1912-0623", "700 5th Ave", "Seattle", "WA", "98101"),
    };

    /// <summary>
    /// Normalize a string for comparison: collapse whitespace and case-fold.
    /// </summary>
    private static string Norm(string value) =>
        string.Join(" ", (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant();

    /// <summary>
    /// Return the customer record whose <c>customer_id</c> matches, or
    /// <c>null</c>. Comparison is whitespace- and case-insensitive.
    /// </summary>
    public static Customer? FindCustomerById(string customerId)
    {
        var target = Norm(customerId);
        return Customers.FirstOrDefault(c => Norm(c.CustomerId) == target);
    }

    /// <summary>
    /// Return a customer matching all supplied identity and address fields.
    ///
    /// All fields must match (whitespace- and case-insensitive). When found,
    /// the returned record carries convenience <c>Name</c> and <c>Address</c>
    /// strings alongside the source record. Returns <c>null</c> if no record
    /// matches.
    /// </summary>
    public static CustomerMatch? FindCustomer(
        string firstName,
        string lastName,
        string accountNumber,
        string address1,
        string city,
        string state,
        string zip)
    {
        var target = new[]
        {
            Norm(firstName), Norm(lastName), Norm(accountNumber),
            Norm(address1), Norm(city), Norm(state), Norm(zip),
        };
        foreach (var customer in Customers)
        {
            var candidate = new[]
            {
                Norm(customer.FirstName), Norm(customer.LastName), Norm(customer.AccountNumber),
                Norm(customer.Address1), Norm(customer.City), Norm(customer.State), Norm(customer.Zip),
            };
            if (candidate.SequenceEqual(target))
            {
                return new CustomerMatch(
                    customer,
                    $"{customer.FirstName} {customer.LastName}",
                    $"{customer.Address1}, {customer.City}, {customer.State} {customer.Zip}");
            }
        }
        return null;
    }
}
