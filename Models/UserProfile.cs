using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DondeComemos.Models
{
    public class UserProfile
    {
        [Key]
        public string UserId { get; set; } = string.Empty;
        
        [Display(Name = "Nombre de Usuario")]
        public string? NombreUsuario { get; set; }
        
        [Display(Name = "Nombres")]
        public string? Nombres { get; set; }
        
        [Display(Name = "Apellidos")]
        public string? Apellidos { get; set; }
        
        [Display(Name = "Foto de Perfil")]
        public string? FotoPerfil { get; set; }
        
        [Display(Name = "Biografía")]
        [StringLength(500)]
        public string? Biografia { get; set; }
        
        [Display(Name = "Teléfono")]
        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        public string? Telefono { get; set; }
        
        [Display(Name = "Fecha de Nacimiento")]
        [DataType(DataType.Date)]
        public DateTime? FechaNacimiento { get; set; }
        
        [Display(Name = "Ciudad")]
        public string? Ciudad { get; set; }
        
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? UltimaActividad { get; set; }
        
        public string? HistorialBusquedas { get; set; }
        public string? RestaurantesFavoritos { get; set; }
        
        [NotMapped]
        public IFormFile? FotoArchivo { get; set; }
    }
}