# Autenticación en el front (registro, login, Google) y correo SMTP

Guía para integrar tu aplicación cliente con la API Pulse: flujos REST, OAuth con Google, CORS y envío real de correos (confirmación de cuenta).

## Convenciones de la API

- **Base URL**: `https://tu-api.com` (o `http://localhost:8080` con Docker, `https://localhost:7169` con Visual Studio).
- **Prefijo de versión**: todas las rutas de auth van bajo **`/v1/auth`**.
- **JSON en snake_case**: el servidor serializa propiedades como `email`, `password`, `id_token` (no `Email` ni `IdToken`).

```http
Content-Type: application/json
Accept: application/json
```

## 1. Registro (correo + contraseña)

**`POST /v1/auth/register`**

```json
{
  "email": "usuario@ejemplo.com",
  "password": "ContraseñaSegura8"
}
```

Requisitos típicos de contraseña: mínimo 8 caracteres, mayúscula, minúscula y dígito.

**Respuesta 200**

```json
{
  "message": "Si el correo es válido, recibirás un enlace de confirmación."
}
```

**400**: cuerpo [Problem Details](https://datatracker.ietf.org/doc/html/rfc7807): `title`, `detail` (p. ej. email duplicado o contraseña inválida).

**503**: el servidor no tiene configurado `App:PublicBaseUrl` (no puede armar el enlace del correo).

### Qué hace el servidor

Crea el usuario, genera token de confirmación y envía un **HTML** con enlace a:

`{PublicBaseUrl}/v1/auth/confirm-email?user_id={guid}&token={token_base64url}`

Si **no** hay SMTP configurado (`Email:Host` vacío), el correo no se envía por red: solo se registra en logs (`LoggingEmailSender`).

---

## 2. Confirmar correo

El usuario abre el enlace del correo. Eso es un **GET** a la API:

**`GET /v1/auth/confirm-email?user_id=...&token=...`**

### Opción A: enlace apunta directo a la API

`PublicBaseUrl` es la URL pública de la API (recomendado en móvil/backend-for-frontend). El usuario confirma en el navegador y listo.

### Opción B: SPA (React, Vue, etc.)

1. En el correo, usa un enlace a **tu front**:  
   `https://tu-app.com/confirmar?user_id=...&token=...`
2. En esa ruta, lee los query params y llama desde el cliente:

```http
GET https://tu-api.com/v1/auth/confirm-email?user_id=...&token=...
```

**200**

```json
{ "message": "Correo confirmado. Ya puedes iniciar sesión." }
```

Para cambiar el enlace del correo a tu dominio del front, habría que ajustar el backend (plantilla / `PublicBaseUrl` apuntando al front y que el front reenvíe a la API). Hoy el enlace se arma con `App:PublicBaseUrl` hacia **`/v1/auth/confirm-email`**.

---

## 3. Login (correo + contraseña)

**`POST /v1/auth/login`**

```json
{
  "email": "usuario@ejemplo.com",
  "password": "ContraseñaSegura8"
}
```

**200**

```json
{
  "access_token": "<JWT>",
  "token_type": "Bearer",
  "expires_in": 43200
}
```

`expires_in` está en **segundos** (12 horas).

**401**: credenciales incorrectas o usuario inexistente (sin detalles por seguridad).

**403** (Problem Details): correo **aún no confirmado**; ofrece “Reenviar confirmación”.

### Guardar y usar el token

Guarda `access_token` de forma segura en tu app. En cada petición protegida:

```http
Authorization: Bearer <access_token>
```

Claims útiles del JWT (referencia): `sub` (id usuario), `tenant_id`, `email`, `scope` (p. ej. `sync:read`, `sync:write`).

---

## 4. Reenviar correo de confirmación

**`POST /v1/auth/resend-confirmation`**

```json
{ "email": "usuario@ejemplo.com" }
```

**200**: mensaje genérico (no revela si el email existe).

---

## 5. Login con Google (OAuth2 / OpenID en el cliente)

La API valida el **ID token** JWT que entrega Google al cliente (no el “access token” de APIs de Google).

### 5.1 Google Cloud Console

1. Crea un proyecto (o usa uno existente).
2. **APIs y servicios** → **Credenciales** → **Crear credenciales** → **Id. de cliente de OAuth**.
3. Tipo **Aplicación web** (si tu front es web) o la configuración que corresponda a tu plataforma.
4. **Orígenes JavaScript autorizados**: URL de tu front (p. ej. `http://localhost:5173`, `https://tu-dominio.com`).
5. **URIs de redireccionamiento** si usas flujo redirect (opcional con GIS One Tap / botón).

### 5.2 Mismo Client ID en API y validación

En el servidor, configura el **mismo Client ID** que usa el front:

```json
"Authentication": {
  "Google": {
    "ClientId": "xxxxx.apps.googleusercontent.com"
  }
}
```

Variable de entorno: `Authentication__Google__ClientId`.

### 5.3 Front web (Google Identity Services)

Carga el script oficial, inicializa con tu `client_id`, y en el callback obtienes `credential` (string JWT):

```html
<script src="https://accounts.google.com/gsi/client" async defer></script>
```

```javascript
// Tras obtener response.credential (id_token JWT del usuario):
const res = await fetch(`${API_BASE}/v1/auth/google`, {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ id_token: response.credential }),
});
const data = await res.json();
// data.access_token — mismo formato que /v1/auth/login
```

**200**: igual que login (`access_token`, `token_type`, `expires_in`).

**401 / 403 / 503**: Problem Details (token inválido, email no verificado en Google, Google no configurado en servidor).

### 5.4 Móvil (iOS / Android)

Usa el SDK de Google Sign-In del sistema, obtén el **ID token** del resultado y envíalo en `POST /v1/auth/google` como `id_token`.

---

## 6. CORS

Si el front y la API están en **orígenes distintos**, el navegador aplicará CORS.

- Si **`Cors:AllowedOrigins`** está **vacío** (`[]`), la API permite **cualquier origen** (útil en desarrollo; en producción conviene restringir).
- En producción, en `appsettings` o variables de entorno:

```json
"Cors": {
  "AllowedOrigins": [ "https://tu-app.com", "https://www.tu-app.com" ]
}
```

Equivalente en variables: repetir el patrón que use tu host para arrays (o un solo origen según cómo inyectes la config). Con JWT en cabecera **no** suele hacer falta `credentials: 'include'` salvo que uses cookies.

Ejemplo `fetch`:

```javascript
fetch(`${API_BASE}/v1/auth/login`, {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ email, password }),
});
```

---

## 7. Correo SMTP (confirmación de registro)

Si **`Email:Host`** y **`Email:FromAddress`** están definidos, la API usa **MailKit** y envía correo real. Si no, solo **log** en consola.

### 7.1 Opciones (`Email`)

| Clave | Descripción |
|--------|-------------|
| `Host` | Servidor SMTP |
| `Port` | Puerto (587 STARTTLS, 465 SSL, 1025 Mailpit) |
| `UserName` / `Password` | Autenticación SMTP (opcional si el relay no exige login) |
| `FromAddress` | Remitente (obligatorio con SMTP) |
| `FromName` | Nombre visible del remitente |
| `SslMode` | `None`, `StartTls` (por defecto), `SslOnConnect` |

Variables de entorno (doble guion bajo):

- `Email__Host`, `Email__Port`, `Email__UserName`, `Email__Password`, `Email__FromAddress`, `Email__FromName`, `Email__SslMode`

**Variables “planas” (Gmail y similares):** si aún no definiste `Email__Host`, la API puede mapear automáticamente:

- `Email` o `EMAIL` / `EMAIL_ADDRESS` → `FromAddress` y usuario SMTP (Gmail suele ser el mismo correo).
- `Host` → `smtp.gmail.com` (u otro SMTP).
- `Port` → `587` (solo se usa cuando ya hay correo + host SMTP detectados; en Railway evita reutilizar la variable `PORT` del servicio web: mejor `Email__Port` o `SMTP_PORT`).
- `Password` → contraseña de aplicación (los espacios internos de Gmail se conservan).

Prioridad: si existe `Email__Host`, no se aplican los alias planos.

### 7.2 Mailpit (desarrollo / Docker)

En este repo, `docker-compose` incluye **Mailpit**:

- Interfaz: **http://localhost:8025**
- SMTP: **localhost:1025** (desde el host) o **`mailpit:1025`** (desde el contenedor `api`)

La API en Compose ya lleva variables `Email__*` apuntando a Mailpit.

### 7.3 Proveedor real (Gmail, SendGrid SMTP, etc.)

Ejemplo genérico STARTTLS (puerto 587):

```json
"Email": {
  "Host": "smtp.tu-proveedor.com",
  "Port": 587,
  "UserName": "usuario",
  "Password": "contraseña-o-app-password",
  "FromAddress": "noreply@tudominio.com",
  "FromName": "Pulse",
  "SslMode": "StartTls"
}
```

**Gmail**: suele requerir [contraseña de aplicación](https://support.google.com/accounts/answer/185833), no la contraseña normal de la cuenta.

### 7.4 Requisitos junto al correo

- **`App:PublicBaseUrl`**: URL pública **accesible desde el navegador del usuario** para que el enlace del correo funcione (en producción, HTTPS de la API o del BFF que exponga `confirm-email`).

---

## 8. Variables resumidas (Railway / Docker)

| Variable | Ejemplo |
|----------|---------|
| `ConnectionStrings__Default` | Cadena Npgsql o `DATABASE_URL` |
| `Jwt__SigningKey` | Clave larga y secreta |
| `App__PublicBaseUrl` | `https://tu-api.up.railway.app` |
| `Authentication__Google__ClientId` | Client ID de Google |
| `Email__Host`, `Email__Port`, … | SMTP de producción o interno |
| `Cors__AllowedOrigins__0` | Origen del front (según binding de arrays en el host) |

---

## 9. Flujo recomendado en tu app con datos locales

1. Pantalla registro / login / Google.
2. Tras login exitoso, guardar `access_token`.
3. Sincronizar datos locales hacia la API con `Authorization: Bearer` en los endpoints de sync.
4. El `tenant_id` del JWT define el espacio de datos en servidor; los usuarios nuevos quedan en el tenant por defecto hasta que definas otro modelo (invitaciones, multi-tenant, etc.).

Si necesitas que el enlace de confirmación abra primero tu app móvil o dominio del front, el siguiente paso es personalizar la generación del URL en el `AuthController` o usar deep links.

---

## 10. Trazabilidad en base de datos (`identity.AspNetUsers`)

Además de `AspNetUserLogins` (proveedor Google + clave), la tabla de usuarios guarda:

| Campo | Uso |
|--------|-----|
| `AuthProvider` | `local` si el alta fue con correo/contraseña; `google` si el primer registro fue solo con Google. Si alguien se registró en local y luego vinculó Google, puede seguir siendo `local`. |
| `GoogleSubject` | Valor `sub` del token de Google (único cuando no es null). |
| `ProfilePictureUrl` | URL de foto; se actualiza en cada login con Google si viene en el token. |
| `CreatedAt` | Alta del usuario (UTC). |
| `LastLoginAt` | Último login exitoso (correo o Google). |

Los informes y auditorías pueden filtrar por estos campos sin unir siempre a `AspNetUserLogins`.
