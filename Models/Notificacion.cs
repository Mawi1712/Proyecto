using System.ComponentModel.DataAnnotations;

namespace DondeComemos.Models
{
    public class Notificacion
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;
        
        [Required]
        [StringLength(1000)]
        public string Mensaje { get; set; } = string.Empty;
        
        [Required]
        public string Tipo { get; set; } = "Info";
        
        public string? Url { get; set; }
        
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        
        public bool Leida { get; set; } = false;
        
        public DateTime? FechaLeida { get; set; }
    }
}