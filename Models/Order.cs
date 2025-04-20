using System;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Models
{
    public partial class Order
    {
        public int    OrderId    { get; set; }
        public int?   OrderNo    { get; set; }
        public int    ProductId  { get; set; }
        public int?   Quantity   { get; set; }
        public string? UserId    { get; set; }
        public string? FullName  { get; set; }    // ← Nouveau
        public string? Address   { get; set; }    // ← Nouveau
        public string? Phone     { get; set; }    // ← Nouveau
        public string? Status    { get; set; }
        public string? PaymentId { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public bool    IsConcel   { get; set; }

        public virtual IdentityUser? User    { get; set; }
        public virtual Product      Product { get; set; } = null!;
        public virtual Payment      Payment { get; set; } = null!;
    }
}
