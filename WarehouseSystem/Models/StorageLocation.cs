using System;
using System.Collections.Generic;

namespace WarehouseSystem.Models;

public partial class StorageLocation
{
    public int LocationId { get; set; }

    public string LocationName { get; set; } = null!;

    public string Type { get; set; } = null!;

    public int Capacity { get; set; }

    public int CurrentOccupancy { get; set; }

    public virtual ICollection<ItemLocation> ItemLocations { get; set; } = new List<ItemLocation>();
}
