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

        // Ces propriétés sont obligatoires et doivent être définies par l'appelant.
        [Required]
        public required string UserId { get; set; }  // Clé étrangère vers IdentityUser

        public DateTime CreatedDate { get; set; }

        // Initialisation de la collection pour éviter les références null.
        public virtual List<CartProduct> CartProducts { get; set; } = new List<CartProduct>();

        [Required]
        public required IdentityUser User { get; set; }

        // Aucun besoin d'initialiser User ou UserId dans le constructeur ici,
        // car ils sont marqués "required" et doivent être fournis lors de l'instanciation.
        public Cart()
        {
            // CartProducts est déjà initialisé.
        }
    }

  public class CartProduct
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CartProductId { get; set; }

        [Required]
        public int CartId { get; set; }

        [Required]
        public int ProductId { get; set; }

        // Initialisé par défaut pour éviter qu'il soit null
        [Required]
        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        // Propriétés de navigation : rendues nullable pour être automatiquement peuplées par EF Core
        public virtual Cart? Cart { get; set; }
        public virtual Product? Product { get; set; }

        public CartProduct()
        {
            ProductName = string.Empty;
        }
    }
}
