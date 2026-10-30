# El Mejor Postor — Proyecto de Software II

Plataforma web de subastas en línea.

| Carpeta | Contenido | Estado |
| --- | --- | --- |
| `frontend/` | Vue 3 + Vite + Bootstrap 5, maquetado igual al diseño de Figma | Conectado a la API |
| `api/` | ASP.NET Core (.NET 10) + EF Core con migraciones | Funcional, con pruebas de las reglas de negocio |

## Documentación

`docs/El-Mejor-Postor-Documentacion.docx` reúne requerimientos, ciclo de vida, diagramas UML y de arquitectura, pruebas y lo que falta. Los diagramas están en `docs/diagramas/` como `.drawio` editables (app.diagrams.net) con su exportación `.png`.

## Arrancar todo con Docker

Solo hace falta Docker. Levanta SQL Server, la API y el frontend:

```bash
docker compose up --build        # http://localhost:8080 · documentación de la API: http://localhost:8080/api/docs
```

La primera vez crea la base y carga los datos de demostración, con subastas en todos los estados: activas (una cierra en menos de 1 hora), programada, vendida con el plazo de pago corriendo, vendida y pagada, y sin ganador. Los datos quedan en volúmenes de Docker; `docker compose down -v` los borra y la próxima vez se siembran de nuevo con fechas frescas. La contraseña de SQL Server se cambia con la variable `SA_PASSWORD`. Es solo para demostración local: corre en modo Development y escucha únicamente en `127.0.0.1`. `GET /api/salud` responde 200 cuando la API llega a SQL Server; Docker lo usa para marcar el contenedor `web` como *healthy*.

## Producción

```bash
cp .env.example .env          # completa SA_PASSWORD, JWT_CLAVE, WEB_URL y SMTP_*
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

`docker-compose.prod.yml` corre en Production: sin datos de demostración ni `/api/docs`, con clave JWT propia y correo por SMTP. Si falta una variable obligatoria, Compose se detiene y dice cuál. El HTTPS lo pone un proxy con certificado delante del puerto `PUERTO_WEB` (Caddy, Traefik o el balanceador del proveedor).

## Frontend

Primero arranca la API (ver abajo) y después:

```bash
cd frontend
npm install
npm run dev                                   # http://localhost:5173
```

En desarrollo, Vite reenvía `/api` y `/fotos` a `http://localhost:5230` (`vite.config.js`); para otra dirección usa la variable `ELMEJORPOSTOR_API`. Para publicar el frontend aparte de la API, compila con `VITE_API_URL=https://tu-api npm run build` y agrega ese origen a `Cors:Origenes`.

| Ruta | Pantalla del Figma |
| --- | --- |
| `/` | 01 Inicio · 09 Móvil · Inicio |
| `/subastas/:id` | 02 Detalle de subasta · 03 Confirmar puja · 03b Puja registrada · 10 Móvil · Subasta |
| `/publicar` | 04 Publicar artículo |
| `/mis-pujas` | 05 Mis pujas |
| `/iniciar-sesion` | 06 Iniciar sesión |
| `/crear-cuenta` | 07 Crear cuenta |
| `/subastas/:id/pagar` | 08 Pagar subasta ganada, adaptada: el ganador y el vendedor ven el contacto del otro |
| `/mis-ventas` | No está en el Figma: subastas publicadas y datos del comprador |
| `/admin` | No está en el Figma: subastas, usuarios y categorías (solo administrador) |
| `/subastas/:id/editar` | Misma pantalla de Publicar, para corregir una subasta sin pujas |
| `/olvide-contrasena` · `/restablecer-contrasena` | No están en el Figma: recuperar la contraseña |
| `/perfil` | No está en el Figma: datos de contacto y cambio de contraseña |
| `/ayuda` | No está en el Figma: cómo pujar, reglas, fotos, pago y entrega, datos personales; la enlazan el pie y el encabezado |

La versión móvil son las mismas pantallas adaptadas al ancho del teléfono. Publicar, Mis pujas, Mis ventas y Pagar piden iniciar sesión. Las pujas, cierres y cambios llegan al instante por SignalR (`/api/tiempo-real`); si la conexión falla, cada pantalla vuelve a consultar la API cada 30 a 60 segundos.

## Pruebas

```bash
cd api && dotnet test                         # 53 pruebas de las reglas de negocio (no necesitan SQL Server)
cd frontend && npx playwright install chromium   # solo la primera vez
cd frontend && npm run e2e                    # 17 pruebas de punta a punta contra http://localhost:8080
```

Las pruebas de punta a punta recorren la aplicación como un usuario: crear cuenta, entrar, publicar con foto, editar y retirar, pujar, puja automática, coordinar la compra, administración y la versión móvil. Necesitan la aplicación levantada (`docker compose up`) y cambian datos; para empezar de cero: `docker compose down -v && docker compose up -d`. Otra dirección: `ELMEJORPOSTOR_URL=http://localhost:5173 npm run e2e`. El informe queda en `frontend/playwright-report`.

GitHub Actions (`.github/workflows/ci.yml`) corre todo esto en cada push y pull request: las pruebas de la API, una revisión de paquetes con vulnerabilidades conocidas, la compilación del frontend y las pruebas de punta a punta sobre `docker compose`.

## API

Usa SQL Server. En desarrollo sirve el contenedor de Docker (`mcr.microsoft.com/mssql/server:2022-latest`, puerto 1433). La cadena de conexión no va en el repositorio: cada uno la guarda con *user-secrets*.

```bash
cd api/src/ElMejorPostor.Api
dotnet user-secrets set "ConnectionStrings:ElMejorPostor" "Server=localhost,1433;Database=ElMejorPostor;User Id=sa;Password=TU_CONTRASEÑA;TrustServerCertificate=True"
dotnet run --launch-profile http -- --sembrar-demo   # la primera vez: crea la base y carga las subastas del Figma
dotnet run --launch-profile http                     # las siguientes: http://localhost:5230
dotnet run --launch-profile http -- --reiniciar-demo # borra la base y vuelve a sembrar con fechas frescas (solo Development)
cd ../.. && dotnet test                              # pruebas de las reglas de negocio (no necesitan SQL Server)
```

Al arrancar aplica las migraciones pendientes (`Datos/Migraciones`). En desarrollo, la documentación interactiva de la API está en `/api/docs` (Scalar): se puede probar cada endpoint, y para los que piden sesión se pega el token de `POST /api/auth/login`. Las pruebas usan SQLite en un archivo temporal con el mismo modelo.

Los usuarios de demostración usan la contraseña de `Demo:Contrasena` en `appsettings.Development.json`: `admin@elmejorpostor.test` (administrador), `maria.r@elmejorpostor.test`, `luis.rosario@elmejorpostor.test`, `ana.pena@elmejorpostor.test`, `jesus.p@elmejorpostor.test`, `carlo91@elmejorpostor.test`, `a.ruiz_rd@elmejorpostor.test`.

**Correo.** «Olvidé mi contraseña» envía el enlace por SMTP con la sección `Smtp` (`Host`, `Puerto`, `Ssl`, `Usuario`, `Contrasena`, `Remitente`); guárdala también con *user-secrets*. Los correos salen en segundo plano y quedan guardados en la tabla `correo_pendiente` hasta que se entregan: si el SMTP falla o la API se reinicia, se reintentan (1, 2, 4… minutos, hasta una hora; se descartan tras 8 intentos). En Development y sin `Smtp:Host`, el correo se escribe en la consola de la API para poder probar el enlace; fuera de Development la API no arranca sin SMTP. `Web:Url` es la dirección del frontend que va en el enlace.

**Avisos por correo.** Se envían cuando te superan (o superan tu puja automática), al cerrar (al ganador y al vendedor, o al vendedor si no hubo ganador), cuando la subasta pasa al siguiente postor por falta de pago y cuando el vendedor confirma el pago.

**Seguridad.** Entrar, registrarse y recuperar la contraseña admiten 10 intentos por minuto por IP (`Limites:CuentaPorMinuto`). Al restablecer la contraseña o desactivar una cuenta se cierran todas sus sesiones abiertas.

| Método y ruta | Qué hace | Sesión |
| --- | --- | --- |
| `POST /api/auth/registro` | Crea la cuenta y devuelve el token | No |
| `POST /api/auth/login` | Inicia sesión | No |
| `POST /api/auth/recuperar` · `POST /api/auth/restablecer` | Envía el enlace para cambiar la contraseña y la cambia con su código (vence en 1 hora, un solo uso) | No |
| `GET /api/auth/yo` | Datos del usuario de la sesión | Sí |
| `GET /api/salud` | 200 si la API está arriba y llega a SQL Server | No |
| `GET /api/categorias` | Categorías con sus subcategorías | No |
| `GET /api/subastas?categoriaId=&buscar=&orden=cierre\|precio\|pujas&pagina=&porPagina=` | Subastas abiertas, por páginas (total en la cabecera `X-Total`) | No |
| `GET /api/subastas/{id}` | Detalle, historial y monto mínimo | No |
| `POST /api/subastas` | Publica un artículo en subasta | Sí |
| `POST /api/subastas/{id}/imagenes` | Sube fotos (JPG o PNG, 10 MB, máximo 10) | Sí, el vendedor |
| `GET /api/subastas/{id}/edicion` · `PUT /api/subastas/{id}` | Datos para editar y guardar cambios, solo sin pujas | Sí, el vendedor |
| `POST /api/subastas/{id}/retirar` | Retira la subasta, solo sin pujas | Sí, el vendedor |
| `DELETE /api/subastas/{id}/imagenes?ruta=` | Quita una foto (siempre queda al menos una) | Sí, el vendedor |
| `POST /api/subastas/{id}/confirmar-pago` | El vendedor confirma que recibió el pago | Sí, el vendedor |
| `POST /api/subastas/{id}/pujas` | Registra una puja; responde si sigues ganando | Sí |
| `PUT /api/subastas/{id}/puja-automatica` · `DELETE` | Programa, cambia o desactiva la puja automática | Sí |
| `GET /api/perfil` · `PUT` · `POST /api/perfil/contrasena` | Ver y editar mis datos; cambiar la contraseña (pide la actual y cierra las demás sesiones) | Sí |
| `GET /api/mis-pujas` | Subastas en las que pujé y cómo voy | Sí |
| `GET /api/mis-ventas` | Subastas que publiqué y, si se vendió, el comprador | Sí |
| `GET /api/subastas/{id}/contacto` | Contacto de la otra parte de una subasta finalizada | Sí, el ganador o el vendedor |
| `GET /api/admin/subastas` | Todas las subastas | Administrador |
| `GET /api/admin/categorias` · `POST` · `PUT /api/admin/categorias/{id}` | Crea, renombra, mueve y oculta categorías | Administrador |
| `GET /api/admin/usuarios` · `PATCH /api/admin/usuarios/{id}` | Lista y activa o desactiva usuarios | Administrador |
| `POST /api/admin/subastas/{id}/cancelar` | Cancela una subasta abierta | Administrador |

Reglas que valida el servidor:
- La primera puja es igual o mayor que el precio de salida; las siguientes, la actual más el incremento mínimo.
- El vendedor no puja en su subasta.
- Ningún monto (precio de salida, incremento, reserva, puja o máximo automático) pasa de RD$ 999,999,999.99.
- No se puja antes del inicio ni después del cierre.
- Una puja en los últimos 2 minutos extiende el cierre 2 minutos.
- Dos pujas simultáneas nunca quedan ambas como la más alta.

- Puja automática: el sistema puja por el postor solo lo necesario para que vaya ganando, sin pasar de su máximo. Entre dos automáticas gana el máximo mayor por el incremento mínimo sobre el otro; en empate, la que se programó primero.
- El vendedor solo edita o retira su subasta mientras nadie haya pujado.
- El ganador tiene 48 horas para pagar. Si el vendedor no confirma el pago en ese plazo, las pujas del ganador se descartan y gana la más alta de otro postor, con 48 horas nuevas; si no queda nadie que alcance la reserva, la subasta queda desierta.
- Las pujas de una cuenta desactivada no cuentan para ganar: al cerrar o al pasar al siguiente se descartan.

Un proceso en segundo plano revisa cada 15 segundos: activa las subastas que llegan a su inicio, cierra las que vencen y aplica el plazo de pago. Al cerrar, gana la puja más alta; si no hubo pujas o no se alcanzó la reserva, la subasta queda desierta.

## Qué falta

- Cobro en línea y comisión: El Mejor Postor no procesa pagos; el ganador y el vendedor se contactan y el vendedor confirma el pago.
- Quedaron fuera, aunque aparecen en el Figma: «Comprar ya», preguntas al vendedor, favoritos, visitas, mensajes, notificaciones dentro de la aplicación e inicio con Google.
