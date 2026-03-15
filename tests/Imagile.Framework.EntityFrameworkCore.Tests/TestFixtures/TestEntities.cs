using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Imagile.Framework.EntityFrameworkCore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Imagile.Framework.EntityFrameworkCore.Tests.TestFixtures;

// ── Soft-delete entities ──────────────────────────────────────────────────────

public class TestPerson : ISoftDeletable
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
}

// ── Reference entities ────────────────────────────────────────────────────────

public class TestCategory : IReferenceEntity<TestCategory>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public string ItemId => Id.ToString();

    public static List<TestCategory> GetSeedData() =>
    [
        new TestCategory { Id = 1, Name = "Electronics" },
        new TestCategory { Id = 2, Name = "Clothing" },
        new TestCategory { Id = 3, Name = "Food" },
    ];
}

// ── Enum lookup entities ──────────────────────────────────────────────────────

public enum OrderStatus
{
    [Description("Waiting for payment")]
    Pending = 0,

    [Description("Payment confirmed")]
    Active = 1,

    Cancelled = 2,
}

public class TestOrder
{
    public int Id { get; set; }
    public OrderStatus Status { get; set; }
    public OrderStatus? NullableStatus { get; set; }
}

// ── String truncation entities ────────────────────────────────────────────────

public class TestProduct
{
    public int Id { get; set; }

    [MaxLength(10)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(5)]
    public string? Code { get; set; }

    // No MaxLength – should never be truncated
    public string Description { get; set; } = string.Empty;
}

// ── Decimal precision entities ────────────────────────────────────────────────

public class TestPriceItem
{
    public int Id { get; set; }

    // No precision attribute – should receive the default
    public decimal Price { get; set; }

    // Has [Precision] – must be skipped by SetDecimalDefaultPrecisionAndScale
    [Precision(10, 4)]
    public decimal ExactPrice { get; set; }
}
