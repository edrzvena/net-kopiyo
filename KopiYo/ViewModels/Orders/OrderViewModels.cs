using System.ComponentModel.DataAnnotations;
using KopiYo.Common;

namespace KopiYo.ViewModels.Orders;

public class OrderListItemViewModel
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal GrandTotal { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public OrderStatus Status { get; set; }
}

public class OrderDetailsViewModel
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public PaymentMethod PaymentMethod { get; set; }

    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ServiceChargePercent { get; set; }
    public decimal ServiceChargeAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal ChangeAmount { get; set; }
    public string? Note { get; set; }

    public DateTime? ReversedAt { get; set; }
    public string? ReversedByName { get; set; }
    public string? ReversalReason { get; set; }

    public List<OrderDetailLineViewModel> Lines { get; set; } = [];
    public List<OrderStockMovementViewModel> StockMovements { get; set; } = [];

    /// <summary>Void hanya untuk kesalahan input di hari yang sama; selebihnya refund.</summary>
    public bool CanVoid { get; set; }
    public bool CanRefund { get; set; }
}

public class OrderDetailLineViewModel
{
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string VariantDescription { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string? Note { get; set; }
}

public class OrderStockMovementViewModel
{
    public string IngredientName { get; set; } = string.Empty;
    public string UnitLabel { get; set; } = string.Empty;
    public StockMovementType MovementType { get; set; }
    public decimal Quantity { get; set; }
    public decimal StockBefore { get; set; }
    public decimal StockAfter { get; set; }
}

public class ReverseOrderViewModel
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal GrandTotal { get; set; }

    /// <summary>true = void (batal hari ini), false = refund (uang dikembalikan).</summary>
    public bool IsVoid { get; set; }

    [Required(ErrorMessage = "Alasan wajib diisi — ini yang tercatat permanen di order.")]
    [StringLength(250)]
    [Display(Name = "Alasan")]
    public string Reason { get; set; } = string.Empty;

    [Display(Name = "Kembalikan stok bahan")]
    public bool RestoreStock { get; set; } = true;
}
