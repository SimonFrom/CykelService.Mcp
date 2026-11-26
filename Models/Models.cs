namespace CykelService.Mcp.Models
{
    public record CustomerSummary(
        long Id,
        string Name,
        string PhoneNumber,
        string Email,
        IReadOnlyList<BikeSummary> Bikes
    );

    public record BikeSummary(long Id, string Make, string Model, string Colour, string Type);

    public record OpenRepair
    {
        public long Id { get; init; }
        public string CustomerName { get; init; } = "";
        public string PhoneNumber { get; init; } = "";
        public string Bike { get; init; } = "";
        public DateTime IntakeDate { get; init; }
        public decimal TotalPrice { get; init; }
        public bool MissingItems { get; init; }
        public bool CustomerContacted { get; init; }
        public string? Note { get; init; }
        public DateTime? DeliveryDate { get; init; }
    }

    public record MonthRevenue
    {
        public int Year { get; init; }
        public int Month { get; init; }
        public int Repairs { get; init; }
        public decimal Total { get; init; }
    }
}
