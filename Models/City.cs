using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PaisApp.Models;

public partial class City
{
    public int Id { get; set; }

    [StringLength(35)]
    public string Name { get; set; } = null!;

    [StringLength(3, MinimumLength = 3)]
    public string CountryCode { get; set; } = null!;

    [StringLength(20)]
    public string District { get; set; } = null!;

    public int Population { get; set; }

    [ValidateNever]
    public virtual Country Country { get; set; } = null!;
}
