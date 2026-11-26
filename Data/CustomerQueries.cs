using CykelService.Mcp.Models;
using Dapper;
using Npgsql;

namespace CykelService.Mcp.Data;

public interface ICustomerQueries
{
    Task<IReadOnlyList<CustomerSummary>> FindAsync(string query, CancellationToken ct);
}

public class CustomerQueries(NpgsqlDataSource db) : ICustomerQueries
{
    private record CustomerRow
    {
        public long Id { get; init; }
        public string FirstName { get; init; } = "";
        public string LastName { get; init; } = "";
        public string PhoneNumber { get; init; } = "";
        public string Email { get; init; } = "";
    }

    private record BikeRow
    {
        public long Id { get; init; }
        public long CustomerId { get; init; }
        public string Make { get; init; } = "";
        public string Model { get; init; } = "";
        public string Colour { get; init; } = "";
        public string Type { get; init; } = "";
    }

    public async Task<IReadOnlyList<CustomerSummary>> FindAsync(string query, CancellationToken ct)
    {
        const string customerSql = """
            select id, first_name, last_name, phone_number, email
            from customers
            where first_name || ' ' || last_name ilike @Pattern
               or phone_number like @Pattern
            order by last_name, first_name
            limit 10
            """;
        const string bikeSql = """
            select id, customer_id, make, model, colour, type
            from bikes
            where customer_id = any(@Ids)
            """;

        await using var conn = await db.OpenConnectionAsync(ct);
        var customers = (await conn.QueryAsync<CustomerRow>(new CommandDefinition(customerSql,
            new { Pattern = $"%{query}%" }, cancellationToken: ct))).ToList();
        if (customers.Count == 0) return [];

        var bikes = (await conn.QueryAsync<BikeRow>(new CommandDefinition(bikeSql,
            new { Ids = customers.Select(c => c.Id).ToArray() }, cancellationToken: ct))).ToList();

        return customers.Select(c => new CustomerSummary(
            c.Id, $"{c.FirstName} {c.LastName}", c.PhoneNumber, c.Email,
            bikes.Where(b => b.CustomerId == c.Id)
                 .Select(b => new BikeSummary(b.Id, b.Make, b.Model, b.Colour, b.Type))
                 .ToList())).ToList();
    }
}