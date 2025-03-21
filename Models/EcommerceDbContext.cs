using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ECommerce.Models;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Models
{
    public class EcommerceDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
    {
        public EcommerceDbContext(DbContextOptions<EcommerceDbContext> options)
            : base(options)
        {
        }
   

        public DbSet<Cart> Carts { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<CartProduct> CartProducts { get; set; }

        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<SubCaregory> SubCaregories { get; set; }
        public DbSet<WishList> WishLists { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Relations entre User et Cart
            modelBuilder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .IsRequired();

            // Relations entre User et Order
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .IsRequired();
            modelBuilder.Entity<ProductReview>()
            .HasKey(pr => pr.ReviewId);
            // Relations entre User et ProductReview
            modelBuilder.Entity<ProductReview>()
                .HasOne(pr => pr.User)
                .WithMany()
                .HasForeignKey(pr => pr.UserId)
                .IsRequired();

            // Relations entre User et WishList
            modelBuilder.Entity<WishList>()
                .HasOne(wl => wl.User)
                .WithMany()
                .HasForeignKey(wl => wl.UserId)
                .IsRequired();
            modelBuilder.Entity<Product>()
           .Property(p => p.Price)
           .HasColumnType("decimal(18,2)");

            // Relations entre Product et Category
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .IsRequired();
            modelBuilder.Entity<CartProduct>()
                .HasKey(cp => new { cp.CartId, cp.ProductId });

            modelBuilder.Entity<CartProduct>()
                .HasOne(cp => cp.Cart)
                .WithMany(c => c.CartProducts)
                .HasForeignKey(cp => cp.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CartProduct>()
                .HasOne(cp => cp.Product)
                .WithMany(p => p.CartProducts)
                .HasForeignKey(cp => cp.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            // Supprime les CartProducts si un Product est supprimé





            // Relations entre Order et Payment
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Payment)
                .WithMany(p => p.Orders)
                .HasForeignKey(o => o.PaymentId)
                .IsRequired();
            modelBuilder.Entity<ProductImage>()
            .HasKey(pi => pi.ImageId);

            // Relations entre Product et ProductImage
            modelBuilder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.ProductImages)
                .HasForeignKey(pi => pi.ProductId)
                .IsRequired();
            modelBuilder.Entity<SubCaregory>()
           .HasKey(sc => sc.SubCategoryId);


            // Relations entre Category et SubCaregory
            modelBuilder.Entity<SubCaregory>()
                .HasOne(sc => sc.Category)
                .WithMany(c => c.SubCaregories)
                .HasForeignKey(sc => sc.CategoryId)
                .IsRequired();

            // Relations entre Product et WishList
            modelBuilder.Entity<WishList>()
                .HasOne(wl => wl.Product)
                .WithMany(p => p.WishLists)
                .HasForeignKey(wl => wl.ProductId)
                .IsRequired();

            
        }

        
    }
}
