using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PaisApp.Models;

public partial class CountryLanguage
{
    [StringLength(3, MinimumLength = 3)]
    public string CountryCode { get; set; } = null!;

    [StringLength(30)]
    public string Language { get; set; } = null!;

    [StringLength(1)]
    public string IsOfficial { get; set; } = null!;

    public decimal Percentage { get; set; }

    [ValidateNever]
    public virtual Country Country { get; set; } = null!;
}
