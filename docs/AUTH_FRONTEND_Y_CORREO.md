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

### Dos JWT distintos (importante)

| Token | Quién lo firma | Para qué sirve |
|--------|------------------|----------------|
| **id_token** de Google | Google | Solo lo envías **una vez** en `POST /v1/auth/google`. La API lo valida con `Authentication:Google:ClientId` y luego **no** lo vuelve a usar. |
| **access_token** de Pulse | Tu API (`JWTKey` / HS256) | Es el **Bearer** en sync y el resto de endpoints. Lo emite el backend tras login Google (o login correo). Claims típicos: `sub` (usuario), `tenant_id`, `scope`, `email`. |

El móvil debe **guardar `access_token`** de la respuesta de `/v1/auth/google` y usar **ese** string en `Authorization: Bearer …` para `POST /v1/sync/...`. Si envías el id_token de Google como Bearer, la API lo rechazará (401).

La validación del Bearer y la firma del token de sesión usan la **misma** configuración `Jwt` (`Issuer`, `Audience`, `SigningKey` / `JWTKey`).

---

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

### 5.4 Móvil (Expo / React Native / nativo)

Usa el SDK de Google Sign-In, obtén el **ID token** del resultado y envíalo en `POST /v1/auth/google` como `id_token`.

En muchos tutoriales de móvil se usa el **Client ID de tipo “Aplicación web”** en `GoogleSignin.configure` (p. ej. `EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID`), porque el token que obtienes debe llevar como **audience** (`aud`) ese mismo ID. Ese **mismo valor** tiene que estar en el backend Pulse como `Authentication__Google__ClientId`. Si el móvil usara solo el Client ID “Android/iOS” y el backend esperara otro, la validación fallaría.

### 5.5 Error «Google no configurado» (503)

Significa que en el **servidor** Pulse **no** está definido `Authentication:Google:ClientId` (o está vacío). El API necesita ese valor para validar el `id_token` con `GoogleJsonWebSignature` (el `aud` del JWT debe coincidir con ese Client ID).

**No basta** con configurar solo la app móvil o el front: quien despliega el API debe definir la variable en el entorno donde corre Pulse:

```bash
Authentication__Google__ClientId=123456789-xxxx.apps.googleusercontent.com
```

En `appsettings.json`:

```json
"Authentication": {
  "Google": {
    "ClientId": "123456789-xxxx.apps.googleusercontent.com"
  }
}
```

Tras cambiar la variable, **reinicia** el servicio del API para que cargue la configuración.

Si el flujo en el teléfono muestra la cuenta de Google y obtienes un token, pero el backend responde 503 con ese mensaje, el fallo es **solo** de configuración del servidor Pulse, no del cliente.

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
| `JWTKey` o `Jwt__SigningKey` | Secreto HMAC para firmar/validar JWT con **HS256**. Debe tener **al menos 32 bytes en UTF-8** (256 bits); si es más corta, verás `IDX10720` al emitir o validar tokens. Si defines `JWTKey`, la API la copia a `Jwt__SigningKey` al arrancar. |
| `App__PublicBaseUrl` | `https://tu-api.up.railway.app` |
| `Authentication__Google__ClientId` | Client ID de Google |
| `Email__Host`, `Email__Port`, … | SMTP de producción o interno |
| `Cors__AllowedOrigins__0` | Origen del front (según binding de arrays en el host) |

---

## 9. Sync devuelve 401 con Bearer (no es el JSON del body)

El servidor **no “omite” el front**: si el POST llega con `Authorization: Bearer …` y la respuesta es **401**, el **JWT no pasó la validación** (falla antes del controlador). El cuerpo `items` / snake_case suele estar bien.

Comprueba en este orden:

1. **Mismo origen que al login**  
   El token debe obtenerse llamando a **la misma base URL** que usas para sync (p. ej. todo `https://apipulse.up.railway.app`). Si iniciaste sesión contra **localhost** u otro despliegue y luego sincronizas contra Railway, la **firma o la clave** no coinciden → 401.

2. **`JWTKey` / `Jwt__SigningKey` idénticos**  
   Quien firmó el token (mismo servicio en el mismo despliegue) debe usar **exactamente** la misma clave que valida las peticiones. Si en Railway rotaste la variable sin volver a iniciar sesión, el token antiguo queda inválido.

3. **`Jwt__Issuer` y `Jwt__Audience`**  
   Deben coincidir con los claims `iss` y `aud` del token (por defecto en el repo: `pulse` y `yapa`). Si en el servidor pusiste otros valores, el login y el sync deben usar **esa** configuración.

4. **Expiración**  
   El token dura 12 horas. Prueba de nuevo tras un login reciente.

5. **Cabecera**  
   Debe ser exactamente `Authorization: Bearer <token>` (un espacio, sin comillas ni saltos de línea dentro del token).

En el servidor, con la versión actual del API, los logs muestran una línea **`JWT rechazado: …`** con el mensaje interno (p. ej. firma, audiencia o expiración). Para ver más detalle en cabeceras HTTP puedes poner temporalmente `Jwt__IncludeErrorDetails=true` en Railway (solo depuración).

---

## 10. Flujo recomendado en tu app con datos locales

1. Pantalla registro / login / Google.
2. Tras login exitoso, guardar `access_token`.
3. Sincronizar datos locales hacia la API con `Authorization: Bearer` en los endpoints de sync.
4. El `tenant_id` del JWT define el espacio de datos en servidor; los usuarios nuevos quedan en el tenant por defecto hasta que definas otro modelo (invitaciones, multi-tenant, etc.).

Si necesitas que el enlace de confirmación abra primero tu app móvil o dominio del front, el siguiente paso es personalizar la generación del URL en el `AuthController` o usar deep links.

---

## 11. Trazabilidad en base de datos (`identity.users`)

Además de `identity.user_logins` (proveedor Google + clave), la tabla `identity.users` guarda:

| Campo | Uso |
|--------|-----|
| `AuthProvider` | `local` si el alta fue con correo/contraseña; `google` si el primer registro fue solo con Google. Si alguien se registró en local y luego vinculó Google, puede seguir siendo `local`. |
| `GoogleSubject` | Valor `sub` del token de Google (único cuando no es null). |
| `ProfilePictureUrl` | URL de foto; se actualiza en cada login con Google si viene en el token. |
| `CreatedAt` | Alta del usuario (UTC). |
| `LastLoginAt` | Último login exitoso (correo o Google). |

Los informes y auditorías pueden filtrar por estos campos sin unir siempre a `user_logins`.

### Nombres de tablas Identity

En el esquema `identity` las tablas usan nombres cortos (`users`, `roles`, `user_logins`, …), no los prefijos por defecto de .NET (`AspNetUsers`, …). El motor sigue siendo **ASP.NET Identity + EF Core**; solo cambia el mapeo a PostgreSQL para que el esquema sea más legible.
