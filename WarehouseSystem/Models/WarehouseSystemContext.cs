using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace WarehouseSystem.Models;

public partial class WarehouseSystemContext : DbContext
{
    public WarehouseSystemContext()
    {
    }

    public WarehouseSystemContext(DbContextOptions<WarehouseSystemContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Item> Items { get; set; }

    public virtual DbSet<ItemLocation> ItemLocations { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<StorageLocation> StorageLocations { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=HP_DATORN\\SQLEXPRESS;Database=WarehouseSystem;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>(entity =>
        {
            entity.Property(e => e.ItemId).HasColumnName("ItemID");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.ExpirationDate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.SerialNumber).HasMaxLength(50);
            entity.Property(e => e.Type).HasMaxLength(50);
        });

        modelBuilder.Entity<ItemLocation>(entity =>
        {
            entity.HasKey(e => e.MappingId);

            entity.Property(e => e.MappingId).HasColumnName("MappingID");
            entity.Property(e => e.AssignedDate).HasColumnType("datetime");
            entity.Property(e => e.ItemId).HasColumnName("ItemID");
            entity.Property(e => e.LocationId).HasColumnName("LocationID");

            entity.HasOne(d => d.Item).WithMany(p => p.ItemLocations)
                .HasForeignKey(d => d.ItemId)
                .HasConstraintName("FK_ItemLocations_Items");

            entity.HasOne(d => d.Location).WithMany(p => p.ItemLocations)
                .HasForeignKey(d => d.LocationId)
                .HasConstraintName("FK_ItemLocations_StorageLocations");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.OrderDate).HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(50);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(e => e.OrderItemId).HasColumnName("OrderItemID");
            entity.Property(e => e.ItemId).HasColumnName("ItemID");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");

            entity.HasOne(d => d.Item).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Items");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Orders");
        });

        modelBuilder.Entity<StorageLocation>(entity =>
        {
            entity.HasKey(e => e.LocationId);

            entity.Property(e => e.LocationId).HasColumnName("LocationID");
            entity.Property(e => e.LocationName).HasMaxLength(50);
            entity.Property(e => e.Type).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
