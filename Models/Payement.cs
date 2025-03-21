using System;
using System.Collections.Generic;

namespace ECommerce.Models
{
    public class Payment
    {
        public string PaymentId { get; set; } // Identifiant unique pour le paiement

        // Informations sur le paiement spécifique (carte bancaire)
        public string? Name { get; set; } // Nom du titulaire de la carte
        public string? CardNo { get; set; } // Numéro de la carte
        public string? ExpiryDate { get; set; } // Date d'expiration de la carte
        public int? CvvNo { get; set; } // Code de sécurité de la carte (CVV)
        public string? Address { get; set; } // Adresse du client
        public string? PaymentMode { get; set; } // Mode de paiement (par ex., carte, PayPal, etc.)

        // Collection des commandes liées à ce paiement
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}

