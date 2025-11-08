using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DondeComemos.Models
{
    public class Pago
    {
        public int Id { get; set; }
        
        [Required]
        public int ReservaId { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Monto { get; set; }
        
        [Required]
        public string Moneda { get; set; } = "PEN";
        
        [Required]
        public string Estado { get; set; } = "Pendiente"; // Pendiente, Completado, Fallido, Reembolsado
        
        public string? StripeSessionId { get; set; }
        
        public string? StripePaymentIntentId { get; set; }
        
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        
        public DateTime? FechaCompletado { get; set; }
        
        public string? MetodoPago { get; set; }
        
        public string? Descripcion { get; set; }
        
        // Navegación
        public virtual Reserva? Reserva { get; set; }
    }
}