using System.ComponentModel.DataAnnotations;

namespace DondeComemos.Models
{
    public class Sugerencia
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "El título es obligatorio")]
        [Display(Name = "Título de la Sugerencia")]
        [StringLength(100)]
        public string Titulo { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "La sugerencia es obligatoria")]
        [Display(Name = "Sugerencia")]
        [StringLength(1000)]
        public string Contenido { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Categoría")]
        public string Categoria { get; set; } = "General";
        
        [Range(1, 5, ErrorMessage = "La calificación debe ser entre 1 y 5")]
        [Display(Name = "Calificación del Sitio")]
        public int Calificacion { get; set; } = 5;
        
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public bool Aprobado { get; set; } = false;
    }
}