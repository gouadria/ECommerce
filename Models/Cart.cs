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

        // Rendre UserId et User non-nullables, mais initialiser dans le constructeur si nécessaire
        [Required]
        public string UserId { get; set; }  // Clé étrangère vers IdentityUser

        public DateTime CreatedDate { get; set; }

        // Liste des produits dans le panier (relation avec Product)
        public virtual List<CartProduct> CartProducts { get; set; }

        // Rendre User non-nullable et initialisé dans le constructeur
        [Required]
        public virtual IdentityUser User { get; set; }

        // Constructeur pour initialiser les propriétés
        public Cart()
        {
            // Initialisation explicite de la liste pour éviter les problèmes de nullité
            CartProducts = new List<CartProduct>();
        }
    }

    public class CartProduct
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CartProductId { get; set; }
        
        // Assurez-vous que CartId et ProductId ne sont pas nulls
        [Required]
        public int CartId { get; set; }

        [Required]
        public int ProductId { get; set; }

        // ProductName est maintenant non-nullable avec une valeur par défaut si nécessaire
        [Required]
        public string ProductName { get; set; }

        public int Quantity { get; set; } // Stockage de la quantité ici

        // Relations
        [Required]  // Cette annotation garantit que Cart ne peut pas être null
        public virtual Cart Cart { get; set; }

        [Required]  // Cette annotation garantit que Product ne peut pas être null
        public virtual Product Product { get; set; }

        // Constructeur pour garantir que les propriétés non-nullables sont initialisées
        public CartProduct()
        {
            ProductName = string.Empty;  // Initialisation à une valeur par défaut si nécessaire
        }
    }
}
