using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DondeComemos.Models
{
    public class Resena
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public int RestauranteId { get; set; }
        
        [Required(ErrorMessage = "El título es obligatorio")]
        [StringLength(100)]
        [Display(Name = "Título de la Reseña")]
        public string Titulo { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "El comentario es obligatorio")]
        [StringLength(1000)]
        [Display(Name = "Comentario")]
        public string Comentario { get; set; } = string.Empty;
        
        [Required]
        [Range(0.5, 5.0, ErrorMessage = "La calificación debe estar entre 0.5 y 5")]
        [Display(Name = "Calificación")]
        public double Calificacion { get; set; }
        
        [Display(Name = "Calidad de Comida")]
        [Range(0.5, 5.0)]
        public decimal? CalidadComida { get; set; }
        
        [Display(Name = "Servicio")]
        [Range(0.5, 5.0)]
        public decimal? Servicio { get; set; }
        
        [Display(Name = "Ambiente")]
        [Range(0.5, 5.0)]
        public decimal? Ambiente { get; set; }
        
        [Display(Name = "Relación Calidad-Precio")]
        [Range(0.5, 5.0)]
        public decimal? RelacionPrecio { get; set; }
        
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        
        [Display(Name = "Verificado")]
        public bool Verificado { get; set; } = false;
        
        [Display(Name = "Aprobado")]
        public bool Aprobado { get; set; } = true;
        
        public virtual Restaurante? Restaurante { get; set; }
        
        [NotMapped]
        public string NombreUsuario { get; set; } = string.Empty;
        
        [NotMapped]
        public string FotoUsuario { get; set; } = string.Empty;
    }
}