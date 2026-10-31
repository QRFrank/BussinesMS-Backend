# Evolution API (WhatsApp) para BussinesMS - guia para Windows

Evolution API es el servicio que envia los mensajes de WhatsApp. Corre en Docker en tu PC, junto con su Postgres y su Redis. Solo escucha en `127.0.0.1:8080` (nadie de afuera puede llamarlo).

> Es una conexion NO oficial (como un WhatsApp Web vinculado). Usa siempre un **numero dedicado** (WhatsApp Business), nunca uno personal.

## Requisitos
- Docker Desktop instalado y abierto. En Settings > General activa **Start Docker Desktop when you sign in** para que arranque solo.

## Paso a paso

Abri PowerShell en esta carpeta (`BackEnd\infra\evolution`).

1. **Copiar el archivo de configuracion**
   ```powershell
   Copy-Item .env.example .env
   ```
2. **Generar la apikey** (una clave larga aleatoria):
   ```powershell
   -join ((48..57)+(65..90)+(97..122) | Get-Random -Count 40 | ForEach-Object {[char]$_})
   ```
   Correlo dos veces: una para la apikey y otra para la clave de Postgres.
3. **Editar `.env`** (Bloc de notas) y reemplazar:
   - `AUTHENTICATION_API_KEY` por la primera clave. Guardala: la vas a pegar en el backend.
   - `POSTGRES_PASSWORD` por la segunda clave, y la **misma** dentro de `DATABASE_CONNECTION_URI` (donde dice `CAMBIAR_POR_OTRA_CLAVE_ALEATORIA`).
   Usa solo letras y numeros para no romper la URI.
4. **Levantar los contenedores**
   ```powershell
   docker compose up -d
   ```
   La primera vez descarga las imagenes (puede tardar unos minutos).
5. **Verificar que levanto**
   ```powershell
   docker compose ps
   docker compose logs -f evolution-api
   ```
   Los 3 servicios deben estar `running` y el log debe terminar mostrando que escucha en el puerto 8080 (Ctrl+C para salir del log). Prueba tambien (reemplaza TU_APIKEY):
   ```powershell
   curl.exe -H "apikey: TU_APIKEY" http://localhost:8080/instance/fetchInstances
   ```
   Debe responder `[]` (lista vacia) y no un error 401.
6. **Conectar el backend** (apikey por user-secrets, nunca en `appsettings.json`). Desde `BackEnd`:
   ```powershell
   dotnet user-secrets init --project src/BussinesMS.API
   dotnet user-secrets set "WhatsApp:Habilitado" "true" --project src/BussinesMS.API
   dotnet user-secrets set "WhatsApp:Evolution:ApiKey" "TU_APIKEY" --project src/BussinesMS.API
   ```
   La URL ya viene en `appsettings.json` (`http://localhost:8080`). En produccion se usan las variables de entorno `WhatsApp__Habilitado` y `WhatsApp__Evolution__ApiKey`.
   Reinicia la API.
7. **Vincular el numero**: en la web, WhatsApp > Numeros y QR > pedir el QR y escanearlo con el celular del numero dedicado (WhatsApp Business > Dispositivos vinculados > Vincular un dispositivo).

## Uso diario
| Accion | Comando |
|---|---|
| Ver estado | `docker compose ps` |
| Ver logs | `docker compose logs -f evolution-api` |
| Apagar (los datos y la sesion quedan) | `docker compose down` |
| Prender de nuevo | `docker compose up -d` |

Los contenedores tienen `restart: unless-stopped`: con Docker Desktop abierto vuelven solos al encender la PC.

## Cuidado
- **`docker compose down -v` borra los volumenes**, incluida la sesion de WhatsApp: habria que volver a escanear el QR. No uses `-v`.
- El celular del numero dedicado tiene que quedar encendido y con datos: si pasa mucho tiempo sin conexion, WhatsApp desvincula el dispositivo.
- Nunca subas el `.env` a git (ya esta en `.gitignore`).
- Si el numero se bloquea: en la web usa **Cambiar numero** y escanea el QR con otro numero dedicado.
