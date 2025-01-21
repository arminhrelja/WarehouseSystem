using System;
using System.Collections.Generic;

namespace WarehouseSystem.Models;

public partial class OrderItem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int ItemId { get; set; }

    public int QuantityOrdered { get; set; }

    public virtual Item Item { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
