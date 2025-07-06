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
		protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
		{
			
			configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
			base.ConfigureConventions(configurationBuilder);
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
		public DbSet<SubCategory> SubCategories { get; set; }
		public DbSet<ProductInventory> ProductInventory { get; set; }
		public DbSet<PaymentMethod> PaymentMethods { get; set; }
		public DbSet<PaymentProvider> PaymentProviders { get; set; }
		public DbSet<Warehouse> Warehouses { get; set; }
		public DbSet<Image> Images { get; set; }
		public DbSet<ProductVariant> ProductVariants { get; set; }
		public DbSet<Collection> Collections { get; set; }
		public DbSet<ProductCollection> ProductCollections { get; set; }
		public DbSet<Review> Reviews { get; set; }
		public DbSet<ReturnRequest> ReturnRequests { get; set; }
		public DbSet<ReturnRequestProduct> ReturnRequestProducts { get; set; }
		public DbSet<WishlistItem> WishlistItems { get; set; }
		public DbSet<CustomerAddress> CustomerAddresses { get; set; }
		public DbSet<CartItem> CartItems { get; set; }

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			// Fix for CS0266 and CS1662
			builder.Entity<Item>()
				.HasOne(i => i.Order)
				.WithMany(o => o.Items.Cast<Item>()) 
				.HasForeignKey(i => i.OrderId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Entity<Item>()
				.HasOne(i => i.Product)
				.WithMany()
				.HasForeignKey(i => i.ProductId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}
}
