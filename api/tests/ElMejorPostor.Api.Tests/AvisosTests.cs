using Microsoft.Extensions.Configuration;
using ElMejorPostor.Api.Servicios;

namespace ElMejorPostor.Api.Tests;

public class AvisosTests : IDisposable
{
    private readonly BaseDePrueba _base = new();
    private readonly List<(string Para, string Asunto)> _enviados = [];

    private sealed class CorreoDePrueba(List<(string, string)> enviados) : IEnviadorCorreo
    {
        public Task EnviarAsync(string para, string asunto, string texto, CancellationToken ct = default)
        {
            enviados.Add((para, asunto));
            return Task.CompletedTask;
        }
    }

    private Avisos NuevosAvisos() => new(new CorreoDePrueba(_enviados), new ConfigurationBuilder().Build());

    private async Task PujarAsync(long subastaId, long postorId, decimal monto)
    {
        await using var db = _base.Contexto();
        await new PujaService(db, _base.Reloj, NuevosAvisos()).RegistrarAsync(subastaId, postorId, monto);
    }

    private async Task ProcesarEnAsync(DateTime momento)
    {
        _base.Reloj.Ahora = momento;
        await using var db = _base.Contexto();
        await new CierreService(db, _base.Reloj, NuevosAvisos()).ProcesarAsync();
    }

    private string[] Para(string correo) => _enviados.Where(e => e.Para == correo).Select(e => e.Asunto).ToArray();

    [Fact]
    public async Task Avisa_al_que_iba_ganando_cuando_lo_superan_pero_no_cuando_se_supera_a_si_mismo()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var a = await _base.UsuarioAsync("maria@prueba.test");
        var b = await _base.UsuarioAsync("jesus@prueba.test");
        var s = await _base.SubastaAsync(vendedor);

        await PujarAsync(s.Id, a.Id, 4000);
        await PujarAsync(s.Id, a.Id, 4500);
        Assert.Empty(_enviados);

        await PujarAsync(s.Id, b.Id, 5000);
        Assert.Single(Para("maria@prueba.test"), x => x.StartsWith("Te superaron"));
    }

    [Fact]
    public async Task Al_cerrar_avisa_al_ganador_y_al_vendedor()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var a = await _base.UsuarioAsync("maria@prueba.test");
        var cierre = BaseDePrueba.Hoy.AddHours(1);
        var s = await _base.SubastaAsync(vendedor, cierre: cierre);
        await PujarAsync(s.Id, a.Id, 5000);

        await ProcesarEnAsync(cierre);

        Assert.Single(Para("maria@prueba.test"), x => x.StartsWith("¡Ganaste"));
        Assert.Single(Para("luis@prueba.test"), x => x.StartsWith("Vendiste"));
    }

    [Fact]
    public async Task Si_no_paga_avisa_al_que_pierde_y_al_que_pasa_a_ganar()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var a = await _base.UsuarioAsync("maria@prueba.test");
        var b = await _base.UsuarioAsync("jesus@prueba.test");
        var cierre = BaseDePrueba.Hoy.AddHours(1);
        var s = await _base.SubastaAsync(vendedor, cierre: cierre);
        await PujarAsync(s.Id, b.Id, 4000);
        await PujarAsync(s.Id, a.Id, 5000);
        await ProcesarEnAsync(cierre);
        _enviados.Clear();

        await ProcesarEnAsync(cierre.AddHours(48));

        Assert.Single(Para("maria@prueba.test"), x => x.StartsWith("Venció el plazo"));
        Assert.Single(Para("jesus@prueba.test"), x => x.StartsWith("¡Ahora ganaste"));
        Assert.Single(Para("luis@prueba.test"), x => x.StartsWith("Vendiste"));
    }

    [Fact]
    public async Task Sin_pujas_avisa_al_vendedor_que_no_hubo_ganador()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var cierre = BaseDePrueba.Hoy.AddHours(1);
        await _base.SubastaAsync(vendedor, cierre: cierre);

        await ProcesarEnAsync(cierre);

        Assert.Single(Para("luis@prueba.test"), x => x.Contains("sin ganador"));
    }

    public void Dispose() => _base.Dispose();
}
