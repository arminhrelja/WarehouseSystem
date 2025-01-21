using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseSystem.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WarehouseSystem.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class InputModuleController : ControllerBase
    {
        WarehouseSystemContext _db = new WarehouseSystemContext();

        public class ItemResponseDto
        {
            public int ItemId { get; set; }
            public string Name { get; set; }
            public string SerialNumber { get; set; }
            public int Quantity { get; set; }
            public DateTime? ExpirationDate { get; set; }
            public string Type { get; set; }
            public DateTime? CreatedDate { get; set; }
        }

        public class LocationResponseDto
        {
            public int LocationId { get; set; }
            public string LocationName { get; set; }
            public string Type { get; set; }
            public int Capacity { get; set; }
            public int CurrentOccupancy { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> AddItem([FromBody] ItemLocation itemLocation)
        {
            try
            {
                if (itemLocation == null)
                    return BadRequest(new { Message = "Item location data is required." });

                if (itemLocation.Item == null)
                    return BadRequest(new { Message = "Item data is required." });

                // Dodavanje artikla
                itemLocation.Item.CreatedDate = DateTime.UtcNow;
                _db.Items.Add(itemLocation.Item);
                await _db.SaveChangesAsync();

                // Provera validnosti lokacije
                var location = await _db.StorageLocations.FindAsync(itemLocation.LocationId);
                if (location == null)
                    return BadRequest(new { Message = "Selected location does not exist." });

                if (location.CurrentOccupancy + itemLocation.Item.Quantity > location.Capacity)
                    return BadRequest(new { Message = "Location capacity exceeded." });

                // Dodavanje zapisa u tabelu ItemLocations
                itemLocation.ItemId = itemLocation.Item.ItemId;
                itemLocation.AssignedDate = DateTime.UtcNow;
                _db.ItemLocations.Add(itemLocation);

                // Ažuriranje zauzeća lokacije
                location.CurrentOccupancy += itemLocation.Item.Quantity;
                _db.StorageLocations.Update(location);
                await _db.SaveChangesAsync();

                // Vraćanje DTO modela
                var response = new
                {
                    Item = new ItemResponseDto
                    {
                        ItemId = itemLocation.Item.ItemId,
                        Name = itemLocation.Item.Name,
                        SerialNumber = itemLocation.Item.SerialNumber,
                        Quantity = itemLocation.Item.Quantity,
                        ExpirationDate = itemLocation.Item.ExpirationDate,
                        Type = itemLocation.Item.Type,
                        CreatedDate = itemLocation.Item.CreatedDate
                    },
                    Location = new LocationResponseDto
                    {
                        LocationId = location.LocationId,
                        LocationName = location.LocationName,
                        Type = location.Type,
                        Capacity = location.Capacity,
                        CurrentOccupancy = location.CurrentOccupancy
                    },
                    Message = "Item added and assigned to the selected location successfully."
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return StatusCode(500, new { Message = "An unexpected error occurred. Please try again later." });
            }
        }

        [HttpGet("GetLocationById/{id}")]
        public async Task<IActionResult> GetLocationById(int id)
        {
            try
            {
                var location = await _db.StorageLocations
                    .Where(l => l.LocationId == id)
                    .Select(l => new
                    {
                        l.LocationId,
                        l.LocationName,
                        l.Type,
                        l.Capacity,
                        l.CurrentOccupancy
                    })
                    .FirstOrDefaultAsync();

                if (location == null)
                    return NotFound(new { Message = "Location not found." });

                return Ok(location);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, new { Message = "An error occurred while processing your request." });
            }
        }


        [HttpGet("get-available-locations/{type}")]
        public async Task<IActionResult> GetAvailableLocations(string type)
        {
            try
            {
                var locations = await _db.StorageLocations
                    .Where(l => l.Type == type && l.CurrentOccupancy < l.Capacity)
                    .Select(l => new
                    {
                        l.LocationId,
                        l.LocationName,
                        l.Type,
                        l.Capacity,
                        l.CurrentOccupancy
                    })
                    .ToListAsync();

                if (!locations.Any())
                    return NotFound("No available locations for the specified type.");

                return Ok(locations);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "An error occurred while fetching locations.");
            }
        }

        [HttpGet("GetLocationsByType/{type}")]
        public async Task<IActionResult> GetLocationsByType(string type)
        {
            var locations = await _db.StorageLocations
                .Where(l => l.Type == type)
                .ToListAsync();
            return Ok(locations);
        }

        [HttpGet("GetProductsByLocation/{locationId}")]
        public async Task<IActionResult> GetProductsByLocation(int locationId)
        {
            var products = await _db.ItemLocations
                .Where(il => il.LocationId == locationId)
                .Select(il => new
                {
                    il.Item.ItemId,
                    il.Item.Name,
                    il.Item.SerialNumber,
                    il.Item.Quantity,
                    il.LocationId,
                    LocationName = il.Location.LocationName, // Naziv trenutne lokacije
                    il.Item.Type
                })
                .ToListAsync();

            return Ok(products);
        }

        [HttpGet("GetOrder")]
        public async Task<IActionResult> GetOrder()
        {
            try
            {
                var order = await _db.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Item)
                    .Where(o => o.Status == "Pending") // Pretpostavljamo da postoji status Pending
                    .OrderBy(o => o.OrderDate)        // Dohvat najstarije narudžbe
                    .FirstOrDefaultAsync();

                if (order == null)
                    return NotFound(new { Message = "No pending orders found." });

                var response = new
                {
                    order.OrderId,
                    order.OrderDate,
                    Products = order.OrderItems.Select(oi =>
                    {
                        var locationName = _db.ItemLocations
                            .Where(il => il.ItemId == oi.ItemId)
                            .Select(il => il.Location.LocationName)
                            .FirstOrDefault() ?? "Location not assigned";

                        return new
                        {
                            oi.ItemId,
                            oi.Item.Name,
                            oi.Item.SerialNumber,
                            oi.QuantityOrdered,
                            LocationName = locationName // Povlači naziv lokacije iz povezane tabele
                        };
                    }).ToList()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return StatusCode(500, new { Message = "An unexpected error occurred. Please try again later." });
            }
        }



        [HttpPost("CompleteOrder")]
        public async Task<IActionResult> CompleteOrder([FromBody] List<OrderCompletionDto> pickedItems)
        {
            if (pickedItems == null || !pickedItems.Any())
                return BadRequest(new { Message = "No items were picked." });

            try
            {
                // Logovanje primljenih podataka
                Console.WriteLine("Received JSON payload:");
                foreach (var item in pickedItems)
                {
                    Console.WriteLine($"ItemId: {item.ItemId}, QuantityOrdered: {item.QuantityOrdered}");
                }

                // Obrada svakog artikla
                foreach (var pickedItem in pickedItems)
                {
                    var item = await _db.Items.FindAsync(pickedItem.ItemId);
                    if (item == null) continue;

                    // Ažuriranje količine u tabeli Items
                    item.Quantity -= pickedItem.QuantityOrdered;

                    // Ažuriranje zauzeća u lokaciji
                    var itemLocation = await _db.ItemLocations
                        .Include(il => il.Location)
                        .FirstOrDefaultAsync(il => il.ItemId == pickedItem.ItemId);

                    if (itemLocation?.Location != null)
                    {
                        itemLocation.Location.CurrentOccupancy -= pickedItem.QuantityOrdered;
                        _db.StorageLocations.Update(itemLocation.Location);
                    }

                    _db.Items.Update(item);
                }

                // Dohvatanje narudžbi koje sadrže izabrane proizvode
                var pickedItemIds = pickedItems.Select(pi => pi.ItemId).ToList();
                var order = await _db.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderItems.Any(oi => pickedItemIds.Contains(oi.ItemId)));

                if (order != null)
                {
                    order.Status = "Completed";
                    _db.Orders.Update(order);
                    Console.WriteLine($"Order {order.OrderId} marked as Completed");
                }

                await _db.SaveChangesAsync();

                return Ok(new { Message = "Order completed successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return StatusCode(500, new { Message = "An unexpected error occurred. Please try again later." });
            }
        }



        public class OrderCompletionDto
        {
            public int ItemId { get; set; }
            public int QuantityOrdered { get; set; }
        }




        [HttpPost("UpdateInventory")]
        public async Task<IActionResult> UpdateInventory([FromBody] List<InventoryUpdateDto> updates)
        {
            var changes = new List<object>();

            foreach (var update in updates)
            {
                var item = await _db.Items.FindAsync(update.ItemId);
                if (item != null)
                {
                    var difference = update.Quantity - item.Quantity;

                    item.Quantity = update.Quantity;
                    item.SerialNumber = update.SerialNumber;

                    var oldLocationId = await _db.ItemLocations
                        .Where(il => il.ItemId == item.ItemId)
                        .Select(il => il.LocationId)
                        .FirstOrDefaultAsync();

                    if (oldLocationId != update.LocationId)
                    {
                        var oldLocation = await _db.StorageLocations.FindAsync(oldLocationId);
                        var newLocation = await _db.StorageLocations.FindAsync(update.LocationId);

                        if (oldLocation != null)
                            oldLocation.CurrentOccupancy -= item.Quantity;
                        if (newLocation != null)
                            newLocation.CurrentOccupancy += update.Quantity;

                        _db.StorageLocations.UpdateRange(oldLocation, newLocation);
                    }

                    _db.Items.Update(item);

                    var newLocationName = await _db.StorageLocations
                        .Where(l => l.LocationId == update.LocationId)
                        .Select(l => l.LocationName)
                        .FirstOrDefaultAsync();

                    changes.Add(new
                    {
                        item.ItemId,
                        item.SerialNumber,
                        item.Quantity,
                        NewLocationName = newLocationName
                    });
                }
            }

            await _db.SaveChangesAsync();
            return Ok(new { Message = "Inventory updated successfully.", Changes = changes });
        }


        public class InventoryUpdateDto
        {
            public int ItemId { get; set; }
            public string SerialNumber { get; set; }
            public int Quantity { get; set; }
            public int LocationId { get; set; }
        }




        [HttpGet("items-with-locations")]
        public async Task<IActionResult> GetItemsWithLocations()
        {
            try
            {
                var itemsWithLocations = await _db.Items
                    .Join(_db.ItemLocations, item => item.ItemId, loc => loc.ItemId, (item, loc) => new { item, loc })
                    .Join(_db.StorageLocations, il => il.loc.LocationId, loc => loc.LocationId, (il, loc) => new
                    {
                        il.item.ItemId,
                        il.item.Name,
                        il.item.SerialNumber,
                        il.item.Quantity,
                        il.item.ExpirationDate,
                        il.item.Type,
                        LocationName = loc.LocationName
                    })
                    .ToListAsync();

                return Ok(itemsWithLocations);
            }
            catch (Exception ex)
            {
                // Logovanje greške (opcionalno dodati pravi loger)
                Console.WriteLine(ex.Message);

                // Vraćanje greške
                return StatusCode(500, "An error occurred while fetching data.");
            }
        }

        [HttpPut("update-item/{id}")]
        public async Task<IActionResult> UpdateItem(int id, [FromBody] Item updatedItem)
        {
            var item = await _db.Items.FindAsync(id);
            if (item == null)
                return NotFound("Item not found.");

            // Ažuriranje artikla
            item.Name = updatedItem.Name;
            item.SerialNumber = updatedItem.SerialNumber;
            item.Quantity = updatedItem.Quantity;
            item.ExpirationDate = updatedItem.ExpirationDate;
            item.Type = updatedItem.Type;

            _db.Items.Update(item);

            // Promena lokacije ako je potrebno
            var currentLocation = await _db.ItemLocations.FirstOrDefaultAsync(il => il.ItemId == id);
            if (currentLocation != null)
            {
                var newLocation = await _db.StorageLocations
                    .Where(l => l.Type == updatedItem.Type && l.CurrentOccupancy < l.Capacity)
                    .OrderBy(l => l.CurrentOccupancy)
                    .FirstOrDefaultAsync();

                if (newLocation != null && newLocation.LocationId != currentLocation.LocationId)
                {
                    currentLocation.LocationId = newLocation.LocationId;
                    newLocation.CurrentOccupancy += updatedItem.Quantity;
                    _db.StorageLocations.Update(newLocation);
                }
            }

            await _db.SaveChangesAsync();
            return Ok(item);
        }


    }
}
