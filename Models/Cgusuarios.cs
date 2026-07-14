using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaisApp.Models;

[Table("cgusuarios")]
public class Cgusuarios
{
    [Key]
    [Column("IdUsuario")]
    public int Id { get; set; }

    [Column("Username")]
    [StringLength(8, MinimumLength = 3)]
    [Required(ErrorMessage = "El usuario es requerido.")]
    public string Username { get; set; } = string.Empty;

    [Column("Nombre")]
    [StringLength(30, MinimumLength = 2)]
    [Required(ErrorMessage = "El nombre es requerido.")]
    public string Nombre { get; set; } = string.Empty;

    [Column("Apellido1")]
    [StringLength(30, MinimumLength = 2)]
    [Required(ErrorMessage = "El primer apellido es requerido.")]
    public string Apellido1 { get; set; } = string.Empty;

    [Column("Apellido2")]
    [StringLength(30)]
    public string Apellido2 { get; set; } = string.Empty;

    [Column("Password")]
    [StringLength(60)]
    public string Password { get; set; } = string.Empty;

    [Column("Nivel")]
    [Range(1, 3)]
    [Required]
    public int Nivel { get; set; } = 1;

    [Column("Status")]
    [Range(0, 1)]
    public int Status { get; set; } = 0;

    [NotMapped]
    [StringLength(100)]
    [DataType(DataType.Password)]
    [Required(ErrorMessage = "La contraseña es requerida.")]
    public string? ContrasenaPlana { get; set; }

    [NotMapped]
    [StringLength(100)]
    [DataType(DataType.Password)]
    [Compare("ContrasenaPlana", ErrorMessage = "Las contraseñas no coinciden.")]
    public string? ConfirmarContrasena { get; set; }

    [NotMapped]
    public string? Correo { get; set; }

    [NotMapped]
    public string? Telefono { get; set; }

    [NotMapped]
    public string NombreCompleto => $"{Nombre} {Apellido1} {Apellido2}".Trim();
}
