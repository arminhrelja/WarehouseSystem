using System;
using System.Collections.Generic;

namespace WarehouseSystem.Models;

public partial class Item
{
    public int ItemId { get; set; }

    public string Name { get; set; } = null!;

    public string SerialNumber { get; set; } = null!;

    public int Quantity { get; set; }

    public DateTime? ExpirationDate { get; set; }

    public string Type { get; set; } = null!;

    public DateTime? CreatedDate { get; set; }

    public virtual ICollection<ItemLocation> ItemLocations { get; set; } = new List<ItemLocation>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
