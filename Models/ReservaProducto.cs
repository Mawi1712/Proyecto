using System.ComponentModel.DataAnnotations;

namespace DondeComemos.Models
{
    public class ReservaProducto
    {
        public int Id { get; set; }
        
        [Required]
        public int ReservaId { get; set; }
        
        [Required]
        public int ProductoId { get; set; }
        
        [Required]
        [Range(1, 20)]
        public int Cantidad { get; set; }
        
        public string? NotasProducto { get; set; }
        
        // Navegación
        public virtual Reserva? Reserva { get; set; }
        public virtual Producto? Producto { get; set; }
    }
}