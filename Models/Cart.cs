using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerce.Models
{
    public partial class Cart
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CartId { get; set; }  // Identifiant du panier
        public string UserId { get; set; }  // Clé étrangère vers IdentityUser
        public DateTime CreatedDate { get; set; }

        // Liste des produits dans le panier (relation avec Product)
        public virtual List<CartProduct> CartProducts { get; set; } = new List<CartProduct>();

        public virtual IdentityUser User { get; set; }
    }

    public class CartProduct
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CartProductId { get; set; }
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public String ProductName { get; set; }

        public int Quantity { get; set; } // Stockage de la quantité ici

        // Relations
        public virtual Cart Cart { get; set; }
        public virtual Product Product { get; set; }
    }
}

