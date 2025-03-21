using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Models
{
    public partial class Order
    {
        public int OrderId { get; set; }

        public int? OrderNo { get; set; }

        public int ProductId { get; set; }

        public int? Quantity { get; set; }

        public string UserId { get; set; }

        public string? Status { get; set; }

        public string PaymentId { get; set; }  // Changer le type pour string

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public bool IsConcel { get; set; }

        // Relation avec Payment
        public virtual Payment Payment { get; set; } = null!;

        // Relation avec Product
        public virtual Product Product { get; set; } = null!;

        // Relation avec IdentityUser
        public virtual IdentityUser User { get; set; } = null!;
    }
}

