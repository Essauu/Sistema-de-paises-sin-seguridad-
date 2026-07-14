using System.ComponentModel.DataAnnotations;

namespace PaisApp.Models;

public partial class Country
{
    [StringLength(3, MinimumLength = 3)]
    public string Code { get; set; } = null!;

    [StringLength(52)]
    public string Name { get; set; } = null!;

    public string Continent { get; set; } = null!;

    [StringLength(26)]
    public string Region { get; set; } = null!;

    public decimal SurfaceArea { get; set; }
    public short? IndepYear { get; set; }
    public int Population { get; set; }
    public decimal? LifeExpectancy { get; set; }
    public decimal? GNP { get; set; }
    public decimal? GNPOld { get; set; }

    [StringLength(45)]
    public string LocalName { get; set; } = null!;

    [StringLength(45)]
    public string GovernmentForm { get; set; } = null!;

    [StringLength(60)]
    public string? HeadOfState { get; set; }

    public int? Capital { get; set; }

    [StringLength(2, MinimumLength = 2)]
    public string Code2 { get; set; } = null!;

    [StringLength(255)]
    public string? FlagUrl { get; set; }

    public virtual City? CapitalCity { get; set; }
    public virtual ICollection<City> Cities { get; set; } = new List<City>();
    public virtual ICollection<CountryLanguage> CountryLanguages { get; set; } = new List<CountryLanguage>();
}
