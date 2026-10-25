# Pújalo — Proyecto de Software II

Plataforma web de subastas en línea.

| Carpeta | Contenido | Estado |
| --- | --- | --- |
| `frontend/` | Vue 3 + Vite + Bootstrap 5, maquetado igual al diseño de Figma | Maqueta completa, con datos de ejemplo en el navegador |
| `api/` | ASP.NET Core (.NET 10) + EF Core con migraciones | Primera versión con pruebas; falta conectarla al frontend |

## Frontend

```bash
cd frontend
npm install
npm run dev
```

| Ruta | Pantalla del Figma |
| --- | --- |
| `/` | 01 Inicio · 09 Móvil · Inicio |
| `/subastas/:id` | 02 Detalle de subasta · 03 Confirmar puja · 03b Puja registrada · 10 Móvil · Subasta |
| `/publicar` | 04 Publicar artículo |
| `/mis-pujas` | 05 Mis pujas |
| `/iniciar-sesion` | 06 Iniciar sesión |
| `/crear-cuenta` | 07 Crear cuenta |
| `/subastas/:id/pagar` | 08 Pagar subasta ganada (maqueta: no cobra) |

La versión móvil son las mismas pantallas adaptadas al ancho del teléfono. Los datos están en `frontend/src/data/subastas.js` hasta que se conecte la API.

## API

```bash
cd api
dotnet test                                   # pruebas de las reglas de negocio
cd src/Pujalo.Api
dotnet run --launch-profile http              # http://localhost:5230
dotnet run --launch-profile http -- --sembrar-demo   # solo la primera vez: carga las subastas del Figma
```

Al arrancar aplica las migraciones pendientes (`Datos/Migraciones`). En desarrollo usa SQLite (`pujalo.db`); para SQL Server basta cambiar el proveedor en `Program.cs` y la cadena `ConnectionStrings:Pujalo`, y regenerar la migración.

Los usuarios de demostración usan la contraseña de `Demo:Contrasena` en `appsettings.Development.json`: `admin@pujalo.test` (administrador), `maria.r@pujalo.test`, `luis.rosario@pujalo.test`, `ana.pena@pujalo.test`, `jesus.p@pujalo.test`, `carlo91@pujalo.test`, `a.ruiz_rd@pujalo.test`.

| Método y ruta | Qué hace | Sesión |
| --- | --- | --- |
| `POST /api/auth/registro` | Crea la cuenta y devuelve el token | No |
| `POST /api/auth/login` | Inicia sesión | No |
| `GET /api/auth/yo` | Datos del usuario de la sesión | Sí |
| `GET /api/categorias` | Categorías con sus subcategorías | No |
| `GET /api/subastas?categoriaId=&buscar=&orden=cierre\|precio\|pujas` | Subastas abiertas | No |
| `GET /api/subastas/{id}` | Detalle, historial y monto mínimo | No |
| `POST /api/subastas` | Publica un artículo en subasta | Sí |
| `POST /api/subastas/{id}/imagenes` | Sube fotos (JPG o PNG, 10 MB, máximo 10) | Sí, el vendedor |
| `POST /api/subastas/{id}/pujas` | Registra una puja | Sí |
| `GET /api/mis-pujas` | Subastas en las que pujé y cómo voy | Sí |
| `GET /api/admin/usuarios` · `PATCH /api/admin/usuarios/{id}` | Lista y activa o desactiva usuarios | Administrador |
| `POST /api/admin/subastas/{id}/cancelar` | Cancela una subasta abierta | Administrador |

Reglas que valida el servidor:
- La primera puja es igual o mayor que el precio de salida; las siguientes, la actual más el incremento mínimo.
- El vendedor no puja en su subasta.
- No se puja antes del inicio ni después del cierre.
- Una puja en los últimos 2 minutos extiende el cierre 2 minutos.
- Dos pujas simultáneas nunca quedan ambas como la más alta.

Un proceso en segundo plano revisa cada 15 segundos: activa las subastas que llegan a su inicio y cierra las que vencen. Al cerrar, gana la puja más alta; si no hubo pujas o no se alcanzó la reserva, la subasta queda desierta.

## Qué falta

- Conectar el frontend a la API (sesión, listados, pujas, publicar con fotos).
- Pantallas de administrador: no existen en el Figma.
- En la API quedaron fuera, aunque aparecen en el Figma: «Comprar ya», puja automática, preguntas al vendedor, favoritos, visitas, mensajes, comisión y pagos.
