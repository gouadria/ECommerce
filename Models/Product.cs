using System;
using System.Collections.Generic;

namespace ECommerce.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string ProductName { get; set; }   = string.Empty;

    public string? ShortDescription { get; set; }

    public string? LongDescription { get; set; }

    public string? AdditionalDescription { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public string? Size { get; set; }

    public string? Color { get; set; }

    public string? CompanyName { get; set; }

    public int CategoryId { get; set; }

    public int SubCategory { get; set; }

    public bool IsCustomized { get; set; }

    public bool IsActive { get; set; }
    


    public int PageSize { get; set; }

    public int CurrentPage { get; set; }

    public int TotalPages { get; set; }


    public DateTime CreatedDate { get; set; }

    public virtual List<CartProduct> CartProducts { get; set; } = new List<CartProduct>();

    public virtual Category Category { get; set; } = null!;
    public List<Category> CategoryList { get; set; } = new List<Category>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    public virtual ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();

    public virtual ICollection<WishList> WishLists { get; set; } = new List<WishList>();
}
