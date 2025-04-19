using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Models
{
public class Cart
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CartId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }

    public virtual List<CartProduct> CartProducts { get; set; } = new List<CartProduct>();

    [ForeignKey("UserId")]
    public IdentityUser? User { get; set; } // PAS de new IdentityUser()
}

public class CartProduct
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CartProductId { get; set; }

    public int CartId { get; set; }

    public int ProductId { get; set; }

    public string? ProductName { get; set; }

    public int Quantity { get; set; }

    public virtual Cart? Cart { get; set; }

    public virtual Product? Product { get; set; }
}
}
