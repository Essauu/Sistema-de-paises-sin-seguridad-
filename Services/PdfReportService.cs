using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PaisApp.Models;

namespace PaisApp.Services;

public interface IPdfReportService
{
    byte[] GenerateCountryReport(Country country, City? capital, IEnumerable<CountryLanguage> languages, IEnumerable<City> cities, byte[]? flagBytes = null);
}

public class PdfReportService : IPdfReportService
{
    public PdfReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateCountryReport(Country country, City? capital, IEnumerable<CountryLanguage> languages, IEnumerable<City> cities, byte[]? flagBytes = null)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Header().Element(c => ComponerEncabezado(c, country, flagBytes));
                page.Content().Element(c => ComponerContenido(c, country, capital, languages, cities));
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Página ").SemiBold();
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private void ComponerEncabezado(IContainer container, Country country, byte[]? flagBytes)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("REPORTE DE PAÍS").FontSize(24).Bold().FontColor(Colors.Blue.Darken2);
                col.Item().Text(country.Name).FontSize(16).FontColor(Colors.Grey.Darken2);
                col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Darken1);
            });

            if (flagBytes != null && flagBytes.Length > 0)
            {
                row.ConstantItem(100).Height(70).Image(flagBytes).FitArea();
            }
            else
            {
                row.ConstantItem(100).Height(70).Border(1).BorderColor(Colors.Grey.Lighten2).AlignCenter().AlignMiddle().Text("SIN BANDERA").FontSize(8).FontColor(Colors.Grey.Darken2);
            }
        });
    }

    private void ComponerContenido(IContainer container, Country country, City? capital, IEnumerable<CountryLanguage> languages, IEnumerable<City> cities)
    {
        container.PaddingVertical(20).Column(col =>
        {
            col.Item().Element(c => ComponerInformacionPais(c, country, capital));
            col.Item().PaddingTop(20).Element(c => ComponerTablaIdiomas(c, languages));
            col.Item().PaddingTop(20).Element(c => ComponerTablaCiudades(c, cities));
        });
    }

    private void ComponerInformacionPais(IContainer container, Country country, City? capital)
    {
        container.Column(col =>
        {
            col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).PaddingBottom(10).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("INFORMACIÓN GENERAL").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                });
            });

            col.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Code: {country.Code}").FontSize(11);
                    c.Item().Text($"Name: {country.Name}").FontSize(11);
                    c.Item().Text($"Local Name: {country.LocalName}").FontSize(11);
                    c.Item().Text($"Continent: {country.Continent}").FontSize(11);
                    c.Item().Text($"Region: {country.Region}").FontSize(11);
                    c.Item().Text($"Surface Area: {country.SurfaceArea:N2} km²").FontSize(11);
                    c.Item().Text($"Population: {country.Population:N0}").FontSize(11);
                    c.Item().Text($"Life Expectancy: {country.LifeExpectancy?.ToString("F1") ?? "N/A"} years").FontSize(11);
                    c.Item().Text($"GNP: {country.GNP?.ToString("N2") ?? "N/A"}").FontSize(11);
                    c.Item().Text($"Government: {country.GovernmentForm}").FontSize(11);
                    c.Item().Text($"Head of State: {country.HeadOfState ?? "N/A"}").FontSize(11);
                    c.Item().Text($"Capital: {capital?.Name ?? "N/A"}").FontSize(11);
                    c.Item().Text($"ISO Code 2: {country.Code2}").FontSize(11);
                    c.Item().Text($"Independence Year: {country.IndepYear?.ToString() ?? "N/A"}").FontSize(11);
                });
            });
        });
    }

    private void ComponerTablaIdiomas(IContainer container, IEnumerable<CountryLanguage> languages)
    {
        var languagesList = languages.ToList();
        if (!languagesList.Any()) return;

        container.Column(col =>
        {
            col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).PaddingBottom(10).Text("OFFICIAL LANGUAGES").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);

            col.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Language").FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignCenter().Text("Official").FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("Percentage").FontColor(Colors.White).Bold();
                });

                foreach (var language in languagesList)
                {
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(language.Language);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text(language.IsOfficial == "T" ? "Yes" : "No");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{language.Percentage:F1}%");
                }
            });
        });
    }

    private void ComponerTablaCiudades(IContainer container, IEnumerable<City> cities)
    {
        var citiesList = cities.OrderByDescending(c => c.Population).Take(10).ToList();
        if (!citiesList.Any()) return;

        container.Column(col =>
        {
            col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).PaddingBottom(10).Text("MAIN CITIES (Top 10)").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);

            col.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("City").FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("District").FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("Population").FontColor(Colors.White).Bold();
                });

                foreach (var city in citiesList)
                {
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(city.Name);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(city.District);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{city.Population:N0}");
                }
            });
        });
    }
}
