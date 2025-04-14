using E_Commers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace E_Commers.Context
{
	public class AppDbContext : IdentityDbContext
	{


		public AppDbContext(DbContextOptions options) : base(options)
		{

		}
		public DbSet<Customer> customers { get; set; }
		public DbSet<UserOperationsLog>   userOperationsLogs { get; set; }
		public DbSet<AdminOperationsLog>  adminOperationsLogs { get; set; }
		public DbSet<Cart> Cart { get; set; }
		public DbSet<Order> Orders { get; set; }
		public DbSet<Item> Items { get; set; }
		public DbSet<Product> Products { get; set; }
		public DbSet<Payment> Payments { get; set; }
		public DbSet<Category> Categories { get; set; }
		public DbSet<PaymentMethod> PaymentMethods { get; set; }
		public DbSet<PaymentProvider> PaymentProviders { get; set; }
		public DbSet<Warehouse> warehouses { get; set; }

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);
			builder.Entity<Order>()
				.HasOne(o => o.Payment)
				.WithOne(p => p.Order)
				.HasForeignKey<Payment>(p => p.OrderId);


			builder.Entity<Order>()
				.HasOne(o => o.Customer)
				.WithMany(c => c.Orders)
				.HasForeignKey(o => o.CustomerId);


			builder.Entity<Item>()
				.HasOne(i => i.Order)
				.WithMany(o => o.Items)
				.HasForeignKey(i => i.OrderId);


			builder.Entity<Item>()
				.HasOne(i => i.Product)
				.WithMany()
				.HasForeignKey(i => i.ProductId);

			builder.Entity<Payment>()
				.HasOne(p => p.PaymentMethod)
				.WithMany()
				.HasForeignKey(p => p.PaymentMethodId);


			builder.Entity<Payment>()
				.HasOne(p => p.PaymentProvider)
				.WithMany()
				.HasForeignKey(p => p.PaymentProviderId);

			builder.Entity<Product>()
				.HasOne(p => p.Category)
				.WithMany(c => c.products)
				.HasForeignKey(p => p.CategoryId);

			builder.Entity<ProductInventory>()
				.HasOne(pi => pi.Product)
				.WithMany(p => p.InventoryEntries)
				.HasForeignKey(pi => pi.ProductId);

			builder.Entity<ProductInventory>()
				.HasOne(pi => pi.Warehouse)
				.WithMany(w => w.ProductInventories)
				.HasForeignKey(pi => pi.WarehouseId);

			builder.Entity<Customer>()
				.HasMany(c => c.Addresses)
				.WithOne(a => a.Customer)
				.HasForeignKey(c => c.CustomerId);

			builder.Entity<Customer>()
				.HasMany(c => c.userOperationsLogs)
				.WithOne(a => a.User)
				.HasForeignKey(c => c.UserId);
			builder.Entity<Customer>()
				.HasMany(c => c.adminOperationsLogs)
				.WithOne(a => a.Admin)
				.HasForeignKey(c => c.AdminId);

			builder.Entity<Discount>()
				.HasMany(d => d.products)
				.WithOne(p => p.Discount)
				.HasForeignKey(p => p.DiscountId);
			builder.Entity<Category>()
				.HasIndex(c => c.Name)
				.IsUnique();
			builder.Entity<Warehouse>()
				.HasIndex(c => c.Name)
				.IsUnique();builder.Entity<Product>()
				.HasIndex(c => c.Name)
				.IsUnique();builder.Entity<Discount>()
				.HasIndex(c => c.Name)
				.IsUnique();
		
			
		

		}
	}
}
