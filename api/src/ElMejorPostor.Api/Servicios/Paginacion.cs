using Microsoft.EntityFrameworkCore;

namespace ElMejorPostor.Api.Servicios;

// Listados por páginas: la respuesta sigue siendo una lista y el total va en la cabecera X-Total.
public static class Paginacion
{
    public const string CabeceraTotal = "X-Total";
    public const int PorPaginaMaximo = 100;

    public static async Task<List<T>> PaginarAsync<T>(this IQueryable<T> q, int pagina, int porPagina, HttpResponse respuesta, CancellationToken ct)
    {
        pagina = Math.Max(1, pagina);
        porPagina = Math.Clamp(porPagina, 1, PorPaginaMaximo);
        respuesta.Headers[CabeceraTotal] = (await q.CountAsync(ct)).ToString();
        return await q.Skip((pagina - 1) * porPagina).Take(porPagina).ToListAsync(ct);
    }

    // «100%» o «a_b» se buscan tal cual, sin que % y _ funcionen como comodines de LIKE.
    public static string PatronLike(string texto) =>
        "%" + texto.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[") + "%";
}
