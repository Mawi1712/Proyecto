using System.Text;
using DondeComemos.Models;

namespace DondeComemos.Services
{
    public interface IPdfService
    {
        byte[] GenerarMenuPdf(Restaurante restaurante, List<Producto> productos);
        string GenerarMenuHtml(Restaurante restaurante, List<Producto> productos);
    }

    public class PdfService : IPdfService
    {
        public byte[] GenerarMenuPdf(Restaurante restaurante, List<Producto> productos)
        {
            var html = GenerarMenuHtml(restaurante, productos);
            return Encoding.UTF8.GetBytes(html);
        }

        public string GenerarMenuHtml(Restaurante restaurante, List<Producto> productos)
        {
            var categorias = productos.GroupBy(p => p.Categoria);
            
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='utf-8'>");
            html.AppendLine($"<title>Menú - {restaurante.Nombre}</title>");
            html.AppendLine("<style>");
            html.AppendLine(@"
                body {
                    font-family: 'Arial', sans-serif;
                    margin: 40px;
                    color: #333;
                }
                .header {
                    text-align: center;
                    margin-bottom: 30px;
                    border-bottom: 3px solid #0d6efd;
                    padding-bottom: 20px;
                }
                .header h1 {
                    color: #0d6efd;
                    margin: 0;
                    font-size: 36px;
                }
                .header p {
                    margin: 5px 0;
                    color: #666;
                }
                .category {
                    margin-top: 30px;
                    page-break-inside: avoid;
                }
                .category-title {
                    background: #0d6efd;
                    color: white;
                    padding: 10px 15px;
                    font-size: 22px;
                    font-weight: bold;
                    margin-bottom: 15px;
                    border-radius: 5px;
                }
                .producto {
                    display: flex;
                    justify-content: space-between;
                    padding: 15px 0;
                    border-bottom: 1px solid #e0e0e0;
                }
                .producto:last-child {
                    border-bottom: none;
                }
                .producto-info {
                    flex: 1;
                }
                .producto-nombre {
                    font-size: 18px;
                    font-weight: bold;
                    color: #222;
                    margin-bottom: 5px;
                }
                .producto-descripcion {
                    color: #666;
                    font-size: 14px;
                    line-height: 1.4;
                }
                .producto-precio {
                    font-size: 20px;
                    font-weight: bold;
                    color: #28a745;
                    white-space: nowrap;
                    margin-left: 20px;
                }
                .footer {
                    margin-top: 40px;
                    text-align: center;
                    color: #999;
                    font-size: 12px;
                    border-top: 1px solid #e0e0e0;
                    padding-top: 20px;
                }
                .info-box {
                    background: #f8f9fa;
                    padding: 15px;
                    border-radius: 5px;
                    margin-bottom: 30px;
                }
                .rating {
                    color: #ffc107;
                    font-size: 20px;
                }
            </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            
            // Header
            html.AppendLine("<div class='header'>");
            html.AppendLine($"<h1>{restaurante.Nombre}</h1>");
            html.AppendLine($"<p><strong>📍 {restaurante.Direccion}</strong></p>");
            if (!string.IsNullOrEmpty(restaurante.Telefono))
                html.AppendLine($"<p>📞 {restaurante.Telefono}</p>");
            html.AppendLine($"<p class='rating'>⭐ {restaurante.Rating:F2} / 5.00</p>");
            html.AppendLine("</div>");
            
            // Info box
            html.AppendLine("<div class='info-box'>");
            html.AppendLine($"<p><strong>Descripción:</strong> {restaurante.Descripcion}</p>");
            if (!string.IsNullOrEmpty(restaurante.TipoCocina))
                html.AppendLine($"<p><strong>Tipo de Cocina:</strong> {restaurante.TipoCocina}</p>");
            if (!string.IsNullOrEmpty(restaurante.Horario))
                html.AppendLine($"<p><strong>Horario:</strong> {restaurante.Horario}</p>");
            if (!string.IsNullOrEmpty(restaurante.RangoPrecios))
                html.AppendLine($"<p><strong>Rango de Precios:</strong> {restaurante.RangoPrecios}</p>");
            html.AppendLine("</div>");
            
            // Productos por categoría
            foreach (var categoria in categorias)
            {
                html.AppendLine("<div class='category'>");
                html.AppendLine($"<div class='category-title'>{categoria.Key}</div>");
                
                foreach (var producto in categoria.Where(p => p.Disponible))
                {
                    html.AppendLine("<div class='producto'>");
                    html.AppendLine("<div class='producto-info'>");
                    html.AppendLine($"<div class='producto-nombre'>{producto.Nombre}</div>");
                    if (!string.IsNullOrEmpty(producto.Descripcion))
                        html.AppendLine($"<div class='producto-descripcion'>{producto.Descripcion}</div>");
                    html.AppendLine("</div>");
                    html.AppendLine($"<div class='producto-precio'>S/ {producto.Precio:F2}</div>");
                    html.AppendLine("</div>");
                }
                
                html.AppendLine("</div>");
            }
            
            // Footer
            html.AppendLine("<div class='footer'>");
            html.AppendLine($"<p>Menú generado el {DateTime.Now:dd/MM/yyyy HH:mm}</p>");
            html.AppendLine("<p>DondeComemos - Tu guía gastronómica en Arequipa</p>");
            html.AppendLine("</div>");
            
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            
            return html.ToString();
        }
    }
}