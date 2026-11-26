using System.ComponentModel;
using CykelService.Mcp.Data;
using CykelService.Mcp.Models;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace CykelService.Mcp.Tools;

[McpServerToolType]
public class RepairTools(IRepairQueries repairs)
{
    [McpServerTool(Name = "open_repairs", ReadOnly = true)]
    [Description("Reparationer, der ikke er afsluttede endnu, ældste først. Viser om der mangler dele, og om kunden er kontaktet.")]
    public Task<IReadOnlyList<OpenRepair>> OpenRepairs(CancellationToken ct)
        => repairs.GetOpenRepairsAsync(ct);

    [McpServerTool(Name = "revenue", ReadOnly = true)]
    [Description("Omsætning for afsluttede reparationer i en periode, opdelt på måned. Beløb i DKK efter rabat.")]
    public Task<IReadOnlyList<MonthRevenue>> Revenue(
        [Description("Første dag i perioden, fx 2026-11-01")] DateOnly from,
        [Description("Sidste dag i perioden, fx 2026-11-30")] DateOnly to,
        CancellationToken ct)
    {
        if (from > to)
            throw new McpException("'from' skal ligge før 'to'.");

        return repairs.GetRevenueAsync(from, to, ct);
    }
}