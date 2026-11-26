using CykelService.Mcp.Models;
using Dapper;
using Npgsql;

namespace CykelService.Mcp.Data;

public interface IRepairQueries
{
    Task<IReadOnlyList<OpenRepair>> GetOpenRepairsAsync(CancellationToken ct);
    Task<IReadOnlyList<MonthRevenue>> GetRevenueAsync(DateOnly from, DateOnly to, CancellationToken ct);
    Task<IReadOnlyList<OpenRepair>> GetUnpaidRepairsAsync(CancellationToken ct);
}

public class RepairQueries(NpgsqlDataSource db) : IRepairQueries
{
    public async Task<IReadOnlyList<OpenRepair>> GetOpenRepairsAsync(CancellationToken ct)
    {
        const string sql = """
            select r.id,
                   c.first_name || ' ' || c.last_name as customer_name,
                   c.phone_number,
                   b.make || ' ' || b.model as bike,
                   r.intake_date,
                   r.total_price,
                   r.missing_items,
                   r."customerContacted" as customer_contacted,
                   r.note
            from repairs r
            join customers c on c.id = r.customer_id
            join bikes b on b.id = r.bike_id
            where r.complete = false
            order by r.intake_date
            """;

        await using var conn = await db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<OpenRepair>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<MonthRevenue>> GetRevenueAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        const string sql = """
            select extract(year from r.delivery_date)::int  as year,
                   extract(month from r.delivery_date)::int as month,
                   count(*)::int                             as repairs,
                   coalesce(sum(r.total_price), 0)           as total
            from repairs r
            where r.complete = true
              and r.delivery_date >= @From
              and r.delivery_date <  @ToExclusive
            group by 1, 2
            order by 1, 2
            """;

        await using var conn = await db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<MonthRevenue>(new CommandDefinition(sql,
            new
            {
                From = from.ToDateTime(TimeOnly.MinValue),
                ToExclusive = to.AddDays(1).ToDateTime(TimeOnly.MinValue)
            },
            cancellationToken: ct));
        return rows.ToList();
    }

    // get all unpaid repairs
    public async Task<IReadOnlyList<OpenRepair>> GetUnpaidRepairsAsync(CancellationToken ct)
    {
        const string sql = """
            select r.id,
                   c.first_name || ' ' || c.last_name as customer_name,
                   c.phone_number,
                   b.make || ' ' || b.model as bike,
                   r.intake_date,
                   r.delivery_date,
                   r.total_price,
                   r.missing_items,
                   r."customerContacted" as customer_contacted,
                   r.note
            from repairs r
            join customers c on c.id = r.customer_id
            join bikes b on b.id = r.bike_id
            where r.complete = true
              and r.payment_recieved = false
            order by r.intake_date
            """;

        await using var conn = await db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<OpenRepair>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.ToList();
    }
}