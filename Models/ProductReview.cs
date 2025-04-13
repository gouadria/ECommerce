using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Models;

public partial class ProductReview
{
    public int ReviewId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public int ProductId { get; set; }

    public string? UserId { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual Product Product { get; set; } = null!;


    public virtual IdentityUser User { get; set; } = null!;
}
