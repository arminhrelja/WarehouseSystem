using System;
using System.Collections.Generic;

namespace WarehouseSystem.Models;

public partial class ItemLocation
{
    public int MappingId { get; set; }

    public int? ItemId { get; set; }

    public int? LocationId { get; set; }

    public DateTime? AssignedDate { get; set; }

    public virtual Item? Item { get; set; }

    public virtual StorageLocation? Location { get; set; }
}
