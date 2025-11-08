using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DondeComemos.Models
{
    [Table("Contactos")]
    public class Contacto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string? Nombre { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "El email es obligatorio")]
        public string? Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "El mensaje es obligatorio")]
        public string? Mensaje { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "La satisfacción del 1-10 es obligatoria")]
        public string? Satisfaccion { get; set; } = string.Empty;
        
        public bool InPositive { get; set; } = true;
        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}