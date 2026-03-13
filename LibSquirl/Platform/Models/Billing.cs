using System.Text.Json.Serialization;

namespace LibSquirl.Platform.Models;

public sealed class Plan
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public string Price { get; set; } = string.Empty;

    [JsonPropertyName("quotas")]
    public PlanQuotas? Quotas { get; set; }
}

public sealed class PlanQuotas
{
    [JsonPropertyName("row_reads")]
    public long RowReads { get; set; }

    [JsonPropertyName("row_writes")]
    public long RowWrites { get; set; }

    [JsonPropertyName("databases")]
    public int Databases { get; set; }

    [JsonPropertyName("locations")]
    public int Locations { get; set; }

    [JsonPropertyName("storage")]
    public long Storage { get; set; }

    [JsonPropertyName("groups")]
    public int Groups { get; set; }

    [JsonPropertyName("bytes_synced")]
    public long BytesSynced { get; set; }
}

public sealed class Subscription
{
    [JsonPropertyName("subscription")]
    public string SubscriptionId { get; set; } = string.Empty;

    [JsonPropertyName("plan")]
    public string Plan { get; set; } = string.Empty;

    [JsonPropertyName("timeline")]
    public string Timeline { get; set; } = string.Empty;

    [JsonPropertyName("overages")]
    public bool Overages { get; set; }
}

public sealed class Invoice
{
    [JsonPropertyName("invoice_number")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [JsonPropertyName("amount_due")]
    public string AmountDue { get; set; } = string.Empty;

    [JsonPropertyName("due_date")]
    public string DueDate { get; set; } = string.Empty;

    [JsonPropertyName("paid_at")]
    public string? PaidAt { get; set; }

    [JsonPropertyName("payment_failed_at")]
    public string? PaymentFailedAt { get; set; }
}

public sealed class OrgUsage
{
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;

    [JsonPropertyName("usage")]
    public OrgUsageData Usage { get; set; } = new();
}

public sealed class OrgUsageData
{
    [JsonPropertyName("rows_read")]
    public long RowsRead { get; set; }

    [JsonPropertyName("rows_written")]
    public long RowsWritten { get; set; }

    [JsonPropertyName("storage_bytes")]
    public long StorageBytes { get; set; }

    [JsonPropertyName("databases")]
    public int Databases { get; set; }

    [JsonPropertyName("locations")]
    public int Locations { get; set; }

    [JsonPropertyName("groups")]
    public int Groups { get; set; }

    [JsonPropertyName("bytes_synced")]
    public long BytesSynced { get; set; }
}

public sealed class UpdateOrganizationRequest
{
    [JsonPropertyName("overages")]
    public bool? Overages { get; set; }
}
