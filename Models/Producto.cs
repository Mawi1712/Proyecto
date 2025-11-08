using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DondeComemos.Models
{
    public class Producto
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "El nombre del producto es obligatorio")]
        [Display(Name = "Nombre del Producto")]
        public string Nombre { get; set; } = string.Empty;
        
        [Display(Name = "Descripción")]
        [StringLength(500)]
        public string? Descripcion { get; set; }
        
        [Required(ErrorMessage = "El precio es obligatorio")]
        [Display(Name = "Precio")]
        [Range(0.01, 999999.99, ErrorMessage = "El precio debe ser mayor a 0")]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Precio { get; set; }
        
        [Display(Name = "Categoría")]
        [Required(ErrorMessage = "La categoría es obligatoria")]
        public string Categoria { get; set; } = "Plato Principal";
        
        [Display(Name = "Disponible")]
        public bool Disponible { get; set; } = true;
        
        [Display(Name = "Imagen")]
        public string? ImagenUrl { get; set; }
        
        [Display(Name = "Ingredientes")]
        [StringLength(1000)]
        public string? Ingredientes { get; set; }
        
        [Display(Name = "Alérgenos")]
        [StringLength(500)]
        public string? Alergenos { get; set; }
        
        [Display(Name = "Calorías")]
        public int? Calorias { get; set; }
        
        [Display(Name = "Tiempo de Preparación (min)")]
        public int? TiempoPreparacion { get; set; }
        
        [Display(Name = "Es Vegetariano")]
        public bool EsVegetariano { get; set; } = false;
        
        [Display(Name = "Es Vegano")]
        public bool EsVegano { get; set; } = false;
        
        [Display(Name = "Sin Gluten")]
        public bool SinGluten { get; set; } = false;
        
        [Display(Name = "Picante")]
        public bool Picante { get; set; } = false;
        
        [Display(Name = "Recomendación del Chef")]
        public bool RecomendacionChef { get; set; } = false;
        
        [Display(Name = "Orden")]
        public int Orden { get; set; } = 0;
        
        [Required]
        public int RestauranteId { get; set; }
        
        public virtual Restaurante? Restaurante { get; set; }
        
        [NotMapped]
        [Display(Name = "Subir Imagen")]
        public IFormFile? ImagenArchivo { get; set; }
        
        [NotMapped]
        [Display(Name = "URL de Imagen Externa")]
        public string? ImagenUrlExterna { get; set; }
    }
}