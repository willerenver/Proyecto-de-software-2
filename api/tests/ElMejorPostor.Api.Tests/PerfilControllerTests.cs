using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ElMejorPostor.Api.Contratos;
using ElMejorPostor.Api.Controllers;
using ElMejorPostor.Api.Dominio;
using ElMejorPostor.Api.Servicios;

namespace ElMejorPostor.Api.Tests;

public class PerfilControllerTests : IDisposable
{
    private readonly BaseDePrueba _base = new();
    private readonly PasswordHasher<Usuario> _hasher = new();

    private PerfilController Controlador(Datos.ElMejorPostorDbContext db, long usuarioId)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Clave"] = new string('x', 64), ["Jwt:Emisor"] = "pruebas", ["Jwt:Audiencia"] = "pruebas",
        }).Build();
        var usuario = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString())], "prueba"));
        return new PerfilController(db, _hasher, new TokenService(config, _base.Reloj))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = usuario } },
        };
    }

    private async Task<Usuario> ConContrasenaAsync(string contrasena)
    {
        var u = await _base.UsuarioAsync("maria@prueba.test");
        await using var db = _base.Contexto();
        var guardado = await db.Usuarios.SingleAsync(x => x.Id == u.Id);
        guardado.ContrasenaHash = _hasher.HashPassword(guardado, contrasena);
        await db.SaveChangesAsync();
        return guardado;
    }

    [Fact]
    public async Task Cambiar_la_contrasena_pide_la_actual_y_cierra_las_otras_sesiones()
    {
        var u = await ConContrasenaAsync("vieja1234");

        await using (var db = _base.Contexto())
            await Assert.ThrowsAsync<ReglaException>(() => Controlador(db, u.Id).CambiarContrasena(new CambiarContrasenaRequest("equivocada1", "nueva1234"), default));

        await using (var db = _base.Contexto())
        {
            var sesion = await Controlador(db, u.Id).CambiarContrasena(new CambiarContrasenaRequest("vieja1234", "nueva1234"), default);
            Assert.False(string.IsNullOrEmpty(sesion.Token));
        }

        await using var lectura = _base.Contexto();
        var guardado = await lectura.Usuarios.SingleAsync(x => x.Id == u.Id);
        Assert.Equal(1, guardado.VersionSesion);
        Assert.NotEqual(PasswordVerificationResult.Failed, _hasher.VerifyHashedPassword(guardado, guardado.ContrasenaHash, "nueva1234"));
    }

    [Fact]
    public async Task El_perfil_muestra_la_cedula_enmascarada()
    {
        var u = await _base.UsuarioAsync("maria@prueba.test");
        await using (var db = _base.Contexto())
            await db.Usuarios.Where(x => x.Id == u.Id).ExecuteUpdateAsync(x => x.SetProperty(y => y.Cedula, "00112345678"));

        await using var db2 = _base.Contexto();
        Assert.Equal("001-****567-8", (await Controlador(db2, u.Id).Ver(default)).Cedula);
    }

    public void Dispose() => _base.Dispose();
}
