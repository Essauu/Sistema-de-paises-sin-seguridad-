# Sistema de Países (sin seguridad)

Aplicación web ASP.NET Core 8.0 para la gestión de países, ciudades e idiomas, con generación de reportes PDF y control de acceso basado en niveles de usuario.

## Tecnologías

- **.NET 8.0** — ASP.NET Core MVC
- **MySQL 8.0** — Base de datos relacional
- **Entity Framework Core** — ORM con Pomelo.EntityFrameworkCore.MySql
- **BCrypt.Net-Next** — Hash de contraseñas
- **QuestPDF** — Generación de reportes PDF
- **Bootstrap 5** — Interfaz de usuario
- **jQuery Validation** — Validación del lado cliente
- **flagcdn.com** — Banderas de países vía CDN

## Funcionalidades

### Gestión de Países
- CRUD completo: crear, editar, eliminar y consultar países
- Búsqueda por código, nombre o código ISO de 2 letras
- Paginación integrada
- Subida opcional de banderas (validación de formato y tamaño)
- Visualización de banderas desde flagcdn.com
- Reporte en PDF descargable con información detallada del país, idiomas oficiales y top 10 ciudades más pobladas

### Gestión de Ciudades
- CRUD completo de ciudades asociadas a un país
- Filtro por nombre, distrito y código de país

### Gestión de Idiomas
- CRUD completo de idiomas asociados a un país
- Indicador de idioma oficial (T/F) y porcentaje de hablantes

### Gestión de Usuarios
- CRUD completo de usuarios del sistema (solo nivel Admin)
- Roles: Consulta (nivel 1), Escritura (nivel 2), Admin (nivel 3)

### Autenticación y Autorización
- Inicio de sesión con cookies de autenticación
- Registro de nuevos usuarios
- Validación de sesión activa (evita sesiones duplicadas)
- Cierre de sesión manual
- Políticas de acceso por nivel de usuario:
  - **Lectura**: nivel 1, 2 o 3
  - **Escritura**: nivel 2 o 3
  - **Admin**: nivel 3

### Reportes PDF
- Reporte detallado por país con QuestPDF
- Incluye: datos generales, bandera, idiomas oficiales, top 10 ciudades
- Descarga directa en formato PDF

## Base de Datos

La base de datos usa el dataset mundial `world` de MySQL con tablas:

- `country` — 239 países con datos demográficos y geográficos
- `city` — 4079 ciudades
- `countrylanguage` — 984 idiomas
- `cgusuarios` — Usuarios del sistema

## Requisitos

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (para MySQL)
- O una instancia de MySQL 8.0 accesible

## Configuración

### 1. Iniciar MySQL con Docker

```bash
docker run --name mysql-paises -e MYSQL_ROOT_PASSWORD=root -p 3307:3306 -d mysql:8.0
```

### 2. Crear la base de datos e importar datos

```bash
docker exec -i mysql-paises mysql -uroot -proot -e "CREATE DATABASE paises;"
docker exec -i mysql-paises mysql -uroot -proot paises < ruta/al/world.sql
```

Agregar columna `FlagUrl` a la tabla `country`:

```sql
ALTER TABLE country ADD COLUMN FlagUrl VARCHAR(255) NULL AFTER Code2;
```

### 3. Configurar conexión

Editar `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3307;Database=paises;User=root;Password=root;"
  }
}
```

### 4. Ejecutar la aplicación

```bash
dotnet run
```

La aplicación estará disponible en `http://localhost:5132`.

### 5. Usuario por defecto

| Usuario | Contraseña | Nivel |
|---------|-----------|-------|
| admin   | admin     | 3     |

## Estructura del Proyecto

```
Controllers/
├── AccountController.cs
├── CityController.cs
├── CountryController.cs
├── CountryLanguageController.cs
├── HomeController.cs
└── UsersController.cs

Data/
└── PaisesContext.cs

Middleware/
└── SessionValidationMiddleware.cs

Models/
├── Cgusuarios.cs
├── City.cs
├── Country.cs
├── CountryLanguage.cs
└── ErrorViewModel.cs

Services/
├── FileValidationService.cs
├── PdfReportService.cs
└── SessionService.cs

Views/
├── Account/
├── City/
├── Country/
├── CountryLanguage/
├── Home/
├── Shared/
└── Users/
```
