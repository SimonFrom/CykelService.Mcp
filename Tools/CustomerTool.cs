using System.ComponentModel;
using CykelService.Mcp.Data;
using CykelService.Mcp.Models;
using ModelContextProtocol.Server;

namespace CykelService.Mcp.Tools;

[McpServerToolType]
public class CustomerTools(ICustomerQueries customers)
{
    [McpServerTool(Name = "find_customer", ReadOnly = true)]
    [Description("Finder kunder på navn eller telefonnummer og viser deres cykler. Højst 10 resultater.")]
    public Task<IReadOnlyList<CustomerSummary>> FindCustomer(
        [Description("Hele eller dele af navnet eller telefonnummeret")] string query,
        CancellationToken ct)
        => customers.FindAsync(query, ct);
}