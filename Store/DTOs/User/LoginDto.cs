using System.ComponentModel.DataAnnotations;

namespace Store.DTOs.User;

public class LoginDto
{
    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contrasena es obligatoria.")]
    public string Password { get; set; } = string.Empty;
}
