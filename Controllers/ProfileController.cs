using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DondeComemos.Data;
using DondeComemos.Models;
using DondeComemos.Services;

namespace DondeComemos.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;

        public ProfileController(
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context,
            IFileService fileService)
        {
            _userManager = userManager;
            _context = context;
            _fileService = fileService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            
            if (user == null)
                return NotFound();

            var profile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                profile = new UserProfile
                {
                    UserId = userId,
                    NombreUsuario = user.Email?.Split('@')[0] ?? "Usuario"
                };
                _context.UserProfiles.Add(profile);
                await _context.SaveChangesAsync();
            }

            ViewBag.Email = user.Email;
            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UserProfile model)
        {
            var userId = _userManager.GetUserId(User);
            var profile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
                return NotFound();

            profile.NombreUsuario = model.NombreUsuario;
            profile.Nombres = model.Nombres;
            profile.Apellidos = model.Apellidos;
            profile.Biografia = model.Biografia;
            profile.Telefono = model.Telefono;
            profile.FechaNacimiento = model.FechaNacimiento;
            profile.Ciudad = model.Ciudad;
            profile.UltimaActividad = DateTime.Now;

            if (model.FotoArchivo != null)
            {
                try
                {
                    if (!string.IsNullOrEmpty(profile.FotoPerfil) && 
                        !profile.FotoPerfil.StartsWith("http"))
                    {
                        _fileService.DeleteImage(profile.FotoPerfil);
                    }

                    profile.FotoPerfil = await _fileService.SaveImageAsync(
                        model.FotoArchivo, "perfiles");
                }
                catch (InvalidOperationException ex)
                {
                    TempData["Error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            _context.Update(profile);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Perfil actualizado exitosamente";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Las contraseñas no coinciden";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            
            if (result.Succeeded)
            {
                TempData["Success"] = "Contraseña cambiada exitosamente";
            }
            else
            {
                TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePhoto()
        {
            var userId = _userManager.GetUserId(User);
            var profile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile != null && !string.IsNullOrEmpty(profile.FotoPerfil))
            {
                if (!profile.FotoPerfil.StartsWith("http"))
                {
                    _fileService.DeleteImage(profile.FotoPerfil);
                }
                
                profile.FotoPerfil = null;
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Foto de perfil eliminada";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}