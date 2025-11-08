using System.ComponentModel.DataAnnotations;

namespace DondeComemos.Models
{
    public class DisponibilidadRestaurante
    {
        public int Id { get; set; }
        
        [Required]
        public int RestauranteId { get; set; }
        
        [Required]
        public DayOfWeek DiaSemana { get; set; }
        
        [Required]
        public TimeSpan HoraInicio { get; set; }
        
        [Required]
        public TimeSpan HoraFin { get; set; }
        
        public int CapacidadMaxima { get; set; } = 50;
        
        public bool Activo { get; set; } = true;
        
        // Navegación
        public virtual Restaurante? Restaurante { get; set; }
    }
}