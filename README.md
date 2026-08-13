# Proyecto Store

Backend ASP.NET Core para administrar usuarios y articulos, con PostgreSQL en Neon, autenticacion JWT y almacenamiento de imagenes en Cloudinary.

## Requisitos

- .NET 10 SDK
- PostgreSQL o una base en Neon
- Cuenta de Cloudinary

## Configuracion

1. Copia `Store/.env.example` como `Store/.env`.
2. Completa localmente las variables del archivo `.env`.
3. Aplica las migraciones y ejecuta el backend:

```powershell
cd Store
dotnet restore
dotnet ef database update
dotnet run --launch-profile http
```

Swagger queda disponible en `http://localhost:5162/swagger`.

## Seguridad

El archivo `.env` contiene secretos y esta excluido de Git. Nunca debe subirse al repositorio.
