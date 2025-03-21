using System;
using System.Collections.Generic;

namespace ECommerce.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string CategoryImageUrl { get; set; } = null!;

    public bool IsActive { get; set; }

    public int PageSize { get; set; }

    public int CurrentPage { get; set; } = 1;

    public int TotalPages { get; set; }


    public DateTime CreatedDate { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<SubCaregory> SubCaregories { get; set; } = new List<SubCaregory>();
}
