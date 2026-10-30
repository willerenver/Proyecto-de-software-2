using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using ElMejorPostor.Api.Controllers;
using ElMejorPostor.Api.Dominio;
using ElMejorPostor.Api.Servicios;

namespace ElMejorPostor.Api.Tests;

public class FotosTests : IDisposable
{
    private readonly BaseDePrueba _base = new();
    private readonly Entorno _entorno = new();

    private sealed class Entorno : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.Combine(Path.GetTempPath(), $"elmejorpostor-web-{Guid.NewGuid():N}");
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "pruebas";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Development";
    }

    private static ControllerContext Como(long usuarioId) => new()
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString())], "prueba")),
        },
    };

    private static IFormFile Jpg()
    {
        var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10 };
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "fotos", "foto.jpg");
    }

    private async Task<IReadOnlyList<string>> SubirAsync(long subastaId, long usuarioId)
    {
        await using var db = _base.Contexto();
        var controlador = new SubastasController(db, null!, _base.Reloj, _entorno, null!) { ControllerContext = Como(usuarioId) };
        return (await controlador.SubirImagenes(subastaId, [Jpg()], CancellationToken.None)).Value!;
    }

    [Fact]
    public async Task Con_pujas_no_se_pueden_subir_fotos()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var postor = await _base.UsuarioAsync("maria@prueba.test");
        var s = await _base.SubastaAsync(vendedor);
        await using (var db = _base.Contexto())
            await new PujaService(db, _base.Reloj).RegistrarAsync(s.Id, postor.Id, 4000);

        var error = await Assert.ThrowsAsync<ReglaException>(() => SubirAsync(s.Id, vendedor.Id));
        Assert.Equal(StatusCodes.Status409Conflict, error.EstadoHttp);
    }

    [Fact]
    public async Task En_una_subasta_terminada_no_se_pueden_subir_fotos()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var s = await _base.SubastaAsync(vendedor);
        await using (var db = _base.Contexto())
            await new VendedorService(db, _base.Reloj).RetirarAsync(s.Id, vendedor.Id);

        var error = await Assert.ThrowsAsync<ReglaException>(() => SubirAsync(s.Id, vendedor.Id));
        Assert.Equal(StatusCodes.Status409Conflict, error.EstadoHttp);
    }

    [Fact]
    public async Task Quitar_una_foto_borra_el_archivo()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var s = await _base.SubastaAsync(vendedor);
        await SubirAsync(s.Id, vendedor.Id);
        var rutas = await SubirAsync(s.Id, vendedor.Id);
        var archivo = Path.Combine(_entorno.WebRootPath, rutas[1].TrimStart('/'));
        Assert.True(File.Exists(archivo));

        await using (var db = _base.Contexto())
        {
            var controlador = new MisSubastasController(db, new VendedorService(db, _base.Reloj), _entorno) { ControllerContext = Como(vendedor.Id) };
            await controlador.QuitarImagen(s.Id, rutas[1], CancellationToken.None);
        }

        Assert.False(File.Exists(archivo));
        Assert.True(File.Exists(Path.Combine(_entorno.WebRootPath, rutas[0].TrimStart('/'))));
    }

    public void Dispose()
    {
        _base.Dispose();
        if (Directory.Exists(_entorno.WebRootPath)) Directory.Delete(_entorno.WebRootPath, recursive: true);
    }
}
