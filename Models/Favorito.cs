using System.ComponentModel.DataAnnotations;

namespace DondeComemos.Models
{
    public class Favorito
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public int RestauranteId { get; set; }
        
        public DateTime FechaAgregado { get; set; } = DateTime.Now;
        
        public virtual Restaurante? Restaurante { get; set; }
    }
}