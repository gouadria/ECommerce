using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Models
{
    public class Payment
    {
       [Key]
    public string? PaymentId { get; set; }    // ex. l’ID retourné par PayPal

    public string? UserId { get; set; }       // FK vers AspNetUsers

    public decimal Amount { get; set; }      // Montant total payé

    public DateTime CreatedDate { get; set; }
     public string? Name { get; set; }
    public string? CardNo { get; set; }
    public string? ExpiryDate { get; set; }
    public int? CvvNo { get; set; }
    public string? Address { get; set; }
    public string? PaymentMode { get; set; }

    // Navigation properties
    public virtual IdentityUser? User { get; set; }
    public virtual ICollection<Order>? Orders { get; set; }
    }
}
