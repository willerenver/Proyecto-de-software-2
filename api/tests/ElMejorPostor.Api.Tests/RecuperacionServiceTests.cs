using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ElMejorPostor.Api.Dominio;
using ElMejorPostor.Api.Servicios;

namespace ElMejorPostor.Api.Tests;

public class RecuperacionServiceTests : IDisposable
{
    private readonly BaseDePrueba _base = new();
    private readonly CorreoDePrueba _correo = new();
    private readonly PasswordHasher<Usuario> _hasher = new();

    private sealed class CorreoDePrueba : IEnviadorCorreo
    {
        public List<(string Para, string Texto)> Enviados { get; } = [];
        public Task EnviarAsync(string para, string asunto, string texto, CancellationToken ct = default)
        {
            Enviados.Add((para, texto));
            return Task.CompletedTask;
        }
    }

    private RecuperacionService Servicio(Datos.ElMejorPostorDbContext db) =>
        new(db, _hasher, _correo, new ConfigurationBuilder().Build(), _base.Reloj);

    private string CodigoEnviado() => Regex.Match(_correo.Enviados[^1].Texto, "codigo=([A-Za-z0-9_-]+)").Groups[1].Value;

    [Fact]
    public async Task Con_el_codigo_del_correo_se_cambia_la_contrasena_una_sola_vez()
    {
        var u = await _base.UsuarioAsync("maria@prueba.test");
        await using (var db = _base.Contexto())
            await Servicio(db).SolicitarAsync("MARIA@prueba.test ");
        var codigo = CodigoEnviado();

        await using (var db = _base.Contexto())
            await Servicio(db).RestablecerAsync(codigo, "nueva1234");

        await using (var db = _base.Contexto())
        {
            var guardado = await db.Usuarios.SingleAsync(x => x.Id == u.Id);
            Assert.NotEqual(PasswordVerificationResult.Failed, _hasher.VerifyHashedPassword(guardado, guardado.ContrasenaHash, "nueva1234"));
            Assert.Null(guardado.RecuperacionHash);
            Assert.Equal(1, guardado.VersionSesion);
            await Assert.ThrowsAsync<ReglaException>(() => Servicio(db).RestablecerAsync(codigo, "otra12345"));
        }
    }

    [Fact]
    public async Task El_codigo_vence_en_una_hora()
    {
        await _base.UsuarioAsync("maria@prueba.test");
        await using (var db = _base.Contexto())
            await Servicio(db).SolicitarAsync("maria@prueba.test");

        _base.Reloj.Ahora = BaseDePrueba.Hoy.AddMinutes(61);
        await using var db2 = _base.Contexto();
        await Assert.ThrowsAsync<ReglaException>(() => Servicio(db2).RestablecerAsync(CodigoEnviado(), "nueva1234"));
    }

    [Fact]
    public async Task Un_correo_desconocido_no_envia_nada_ni_da_error()
    {
        await using var db = _base.Contexto();
        await Servicio(db).SolicitarAsync("nadie@prueba.test");
        Assert.Empty(_correo.Enviados);
    }

    public void Dispose() => _base.Dispose();
}
