# FinanzasApi (ApiRestProject)

API REST de finanzas personales construida con **.NET 10** y **SQL Server**, con autenticación **JWT**.
Arquitectura por capas (Domain, Application, Infrastructure, Api/Controllers) y configuración separada por entornos.

---

## Requisitos

- [.NET SDK 10.0](https://dotnet.microsoft.com/download)
- SQL Server (local se asume **SQL Server Express**)
- Opcional para despliegue: [Docker](https://www.docker.com/) y [AWS CLI](https://docs.aws.amazon.com/cli/)

---

## Entornos de configuración

La app carga `appsettings.json` (base) y encima `appsettings.{ASPNETCORE_ENVIRONMENT}.json`.
El sufijo del archivo **debe** coincidir con el valor de la variable `ASPNETCORE_ENVIRONMENT`.

| Entorno | Archivo                   | Uso                          | Secretos                         |
|---------|---------------------------|------------------------------|----------------------------------|
| `dev`   | `appsettings.dev.json`    | Desarrollo local             | Dentro del archivo (solo local)  |
| `stage` | `appsettings.stage.json`  | Despliegue en AWS EC2        | Por **variables de entorno**     |
| (base)  | `appsettings.json`        | Respaldo neutro, sin valores | Ninguno                          |

> El binario compilado (`dotnet ApiRestProject.dll`) **no** lee `launchSettings.json`.
> Si no defines `ASPNETCORE_ENVIRONMENT`, arranca como `Production` y no cargará ni `dev` ni `stage`,
> lo que produce errores como `La clave 'Jwt:Key' debe tener al menos 32 caracteres` o falta de cadena de conexión.

### Claves de configuración

| Clave                        | Descripción                                             |
|------------------------------|---------------------------------------------------------|
| `ConnectionStrings:FinanzasDb` | Cadena de conexión a SQL Server                       |
| `Jwt:Key`                    | Clave de firma JWT (**mínimo 32 caracteres**)           |
| `Jwt:Issuer` / `Jwt:Audience`| Emisor y audiencia del token                            |
| `Jwt:ExpiresMinutes`         | Vigencia del token en minutos (por defecto 480)         |
| `Cors:Origins`               | Orígenes permitidos para el frontend                    |
| `Auth:AllowRegistration`     | Habilita/deshabilita el registro de cuentas             |
| `Seed:AdminEmail` / `Seed:AdminPassword` | Admin inicial creado al arrancar            |

Para variables de entorno, el separador `:` se escribe como doble guion bajo `__`.
Ejemplo: `Jwt:Key` → `Jwt__Key`.

---

## Levantamiento en local (dev)

1. Verifica que SQL Server Express esté corriendo y que la cadena en `appsettings.dev.json` apunte a tu instancia.
2. Desde la carpeta del proyecto:

   ```powershell
   dotnet run --profile dev
   ```

   O en Visual Studio, selecciona el perfil **dev** (o **dev-https**) y ejecuta.

3. La API queda disponible en:
   - HTTP: `http://localhost:5249`
   - HTTPS (perfil `dev-https`): `https://localhost:7051`

4. Comprobaciones rápidas:

   ```powershell
   # Health check (no requiere auth)
   curl http://localhost:5249/health

   # Login para obtener el token JWT
   curl -X POST http://localhost:5249/api/auth/login `
     -H "Content-Type: application/json" `
     -d '{ "email": "admin@finanzas.local", "password": "Admin123*" }'
   ```

5. En desarrollo, la documentación OpenAPI se expone en `/openapi/v1.json`.

> Al arrancar, `DbInitializer` crea/actualiza la base de datos y el administrador inicial de forma idempotente
> (usa `EnsureCreated`, sin migraciones EF).

---

## Despliegue a AWS (EC2 con Docker)

El proyecto incluye un `Dockerfile` multi-stage que publica en Release y expone el puerto **8080**.

### 1. Construir la imagen

```powershell
docker build -t finanzas-api:latest .
```

Prueba local del contenedor (mapea 8080 del contenedor a 5000 del host):

```powershell
docker run --rm -p 5000:8080 `
  -e ASPNETCORE_ENVIRONMENT=stage `
  -e "ConnectionStrings__FinanzasDb=Server=TU_HOST;Database=FinanzasDb;User Id=app;Password=***;Encrypt=True;TrustServerCertificate=True;" `
  -e "Jwt__Key=clave-aleatoria-de-al-menos-32-caracteres" `
  -e "Seed__AdminPassword=password-admin" `
  finanzas-api:latest
```

### 2. Publicar la imagen en Amazon ECR

```bash
# Variables
AWS_ACCOUNT_ID=123456789012
AWS_REGION=us-east-1
REPO=finanzas-api

# Crear el repositorio (una sola vez)
aws ecr create-repository --repository-name $REPO --region $AWS_REGION

# Autenticar Docker contra ECR
aws ecr get-login-password --region $AWS_REGION \
  | docker login --username AWS --password-stdin $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com

# Etiquetar y subir
docker tag finanzas-api:latest $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$REPO:latest
docker push $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$REPO:latest
```

### 3. Preparar la instancia EC2

- Lanza una instancia (Amazon Linux 2023 recomendado).
- En el **Security Group**, abre el puerto de la app (p. ej. 8080) o 80/443 si pones un proxy inverso delante.
- Asigna un **IAM Role** a la instancia con permiso de solo lectura sobre ECR
  (política gestionada `AmazonEC2ContainerRegistryReadOnly`) para poder hacer `docker pull`.
- Instala Docker:

  ```bash
  sudo dnf update -y
  sudo dnf install -y docker
  sudo systemctl enable --now docker
  ```

### 4. Desplegar el contenedor en EC2

Define los secretos como variables de entorno (no van en el repositorio ni en la imagen):

```bash
AWS_ACCOUNT_ID=123456789012
AWS_REGION=us-east-1
REPO=finanzas-api

# Login y pull de la imagen desde ECR
aws ecr get-login-password --region $AWS_REGION \
  | sudo docker login --username AWS --password-stdin $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com
sudo docker pull $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$REPO:latest

# Ejecutar en modo stage
sudo docker run -d --name finanzas-api --restart unless-stopped \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=stage \
  -e "ConnectionStrings__FinanzasDb=Server=TU_RDS_ENDPOINT;Database=FinanzasDb;User Id=app;Password=***;Encrypt=True;TrustServerCertificate=True;" \
  -e "Jwt__Key=clave-aleatoria-de-al-menos-32-caracteres" \
  -e "Seed__AdminPassword=password-admin" \
  -e "Cors__Origins__0=https://tu-frontend.com" \
  $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$REPO:latest
```

> Para la base de datos en AWS se recomienda **Amazon RDS for SQL Server** en lugar de SQL Server dentro de la instancia.
> Apunta `ConnectionStrings__FinanzasDb` al endpoint de RDS y permite el tráfico desde el Security Group de EC2 al de RDS.

### 5. Verificar

```bash
curl http://localhost:8080/health
# o desde fuera:  curl http://<IP-publica-EC2>:8080/health
```

---

## Manejo de secretos (buenas prácticas)

- **Nunca** subas secretos reales a `appsettings.json` ni a la imagen Docker.
- `appsettings.dev.json` contiene credenciales **solo de tu máquina local**; considera añadirlo a `.gitignore`.
- En EC2/stage, inyecta secretos por variables de entorno, o mejor con **AWS Secrets Manager** / **SSM Parameter Store**.
- La `Jwt:Key` debe tener **al menos 32 caracteres**; genera una aleatoria por entorno.

---

## Endpoints principales

| Método | Ruta                      | Auth        | Descripción                     |
|--------|---------------------------|-------------|---------------------------------|
| GET    | `/health`                 | Anónimo     | Estado del servicio             |
| POST   | `/api/auth/login`         | Anónimo     | Inicia sesión, devuelve JWT     |
| POST   | `/api/auth/register`      | Anónimo     | Registro (si está habilitado)   |
| GET    | `/api/auth/me`            | JWT         | Usuario actual                  |
| POST   | `/api/auth/change-password` | JWT       | Cambia la contraseña            |

Resto de módulos: `categories`, `transactions`, `budgets`, `debts`, `savings-plans`,
`fixed-expenses`, `reports`, `dashboard`, `users` (todos bajo `/api`, requieren JWT).

Para llamadas autenticadas, envía el header: `Authorization: Bearer <token>`.
