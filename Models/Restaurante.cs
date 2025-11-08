using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DondeComemos.Models
{
    public class Restaurante
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [Display(Name = "Nombre del Restaurante")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección es obligatoria")]
        [Display(Name = "Dirección")]
        public string Direccion { get; set; } = string.Empty;

        [Display(Name = "URL de Google Maps")]
        public string? GoogleMapsUrl { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria")]
        [Display(Name = "Descripción")]
        [StringLength(1000)]
        public string Descripcion { get; set; } = string.Empty;

        [Display(Name = "URL de Imagen")]
        public string ImagenUrl { get; set; } = string.Empty;

        [Required]
        [Range(0, 5, ErrorMessage = "El rating debe estar entre 0 y 5")]
        [Display(Name = "Calificación")]
        public double Rating { get; set; }

        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [Display(Name = "Horario")]
        public string? Horario { get; set; }

        [Required(ErrorMessage = "El tipo de cocina es obligatorio")]
        [Display(Name = "Tipo de Cocina")]
        public string TipoCocina { get; set; } = "Peruana";

        [Display(Name = "Latitud")]
        [Range(-90, 90)]
        public double? Latitud { get; set; }

        [Display(Name = "Longitud")]
        [Range(-180, 180)]
        public double? Longitud { get; set; }

        [Required(ErrorMessage = "El rango de precios es obligatorio")]
        [Display(Name = "Rango de Precios")]
        public string RangoPrecios { get; set; } = "Medio";

        [Display(Name = "Sitio Web")]
        public string? SitioWeb { get; set; }

        [Display(Name = "Facebook")]
        public string? Facebook { get; set; }

        [Display(Name = "Instagram")]
        public string? Instagram { get; set; }

        [Display(Name = "Twitter")]
        public string? Twitter { get; set; }

        [Display(Name = "Destacado")]
        public bool Destacado { get; set; } = false;

        [Display(Name = "Acepta Reservas")]
        public bool AceptaReservas { get; set; } = false;

        [Display(Name = "Delivery Disponible")]
        public bool DeliveryDisponible { get; set; } = false;

        [Display(Name = "Estacionamiento")]
        public bool TieneEstacionamiento { get; set; } = false;

        [Display(Name = "WiFi Gratis")]
        public bool WifiGratis { get; set; } = false;

        [Display(Name = "Accesible para Discapacitados")]
        public bool AccesibleDiscapacitados { get; set; } = false;

        [Display(Name = "Opciones Vegetarianas")]
        public bool OpcionesVegetarianas { get; set; } = false;

        [Display(Name = "Opciones Veganas")]
        public bool OpcionesVeganas { get; set; } = false;

        [Display(Name = "Ambiente")]
        public string? Ambiente { get; set; }

        public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
        public virtual ICollection<Resena> Resenas { get; set; } = new List<Resena>();

        [NotMapped]
        [Display(Name = "Subir Imagen")]
        public IFormFile? ImagenArchivo { get; set; }

        [NotMapped]
        [Display(Name = "URL de Imagen Externa")]
        public string? ImagenUrlExterna { get; set; }

        [NotMapped]
        public double RatingPromedio => Resenas.Any()
            ? Resenas.Average(r => r.Calificacion)
            : Rating;

        [NotMapped]
        public int TotalResenas => Resenas.Count;

        // Agregar navegación para Reservas
        public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }
}