using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DondeComemos.Models
{
    public class Reserva
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public int RestauranteId { get; set; }
        
        [Required(ErrorMessage = "La fecha es obligatoria")]
        [Display(Name = "Fecha de Reserva")]
        public DateTime FechaReserva { get; set; }
        
        [Required(ErrorMessage = "La hora es obligatoria")]
        [Display(Name = "Hora de Reserva")]
        public TimeSpan HoraReserva { get; set; }
        
        [Required(ErrorMessage = "El número de personas es obligatorio")]
        [Range(1, 20, ErrorMessage = "El número de personas debe estar entre 1 y 20")]
        [Display(Name = "Número de Personas")]
        public int NumeroPersonas { get; set; }
        
        [Display(Name = "Notas Especiales")]
        [StringLength(500)]
        public string? NotasEspeciales { get; set; }
        
        [Required]
        [Display(Name = "Estado")]
        public string Estado { get; set; } = "Pendiente"; // Pendiente, Confirmada, Cancelada, Completada
        
        [Display(Name = "Fecha de Creación")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        
        [Display(Name = "Fecha de Confirmación")]
        public DateTime? FechaConfirmacion { get; set; }
        
        [Display(Name = "Código de Reserva")]
        public string CodigoReserva { get; set; } = string.Empty;
        
        // Navegación
        public virtual Restaurante? Restaurante { get; set; }
        public virtual ICollection<ReservaProducto> ProductosReservados { get; set; } = new List<ReservaProducto>();
        
        [NotMapped]
        public string NombreUsuario { get; set; } = string.Empty;
        
        [NotMapped]
        public string EmailUsuario { get; set; } = string.Empty;
    }
}