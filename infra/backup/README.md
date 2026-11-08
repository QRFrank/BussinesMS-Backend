# Backups de BussinesMS en el VPS

Cada ejecucion deja un `backup-FECHA.tar.gz` con:

- las 3 bases de SQL Server (`BussinesMS_Auth`, `BussinesMS_Sistema`, `BussinesMS_Navidad`) en `.bak` (con `CHECKSUM`; Express no soporta `COMPRESSION`),
- `evolution.dump` (Postgres de Evolution) y `evolution-instances.tgz` (la sesion de WhatsApp).

Se guarda 14 dias en `/var/backups/bussinesms` y se sube cifrado a R2 (`r2cifrado:`). Corre a las 07:00 y 16:00 UTC (03:00 y 12:00 de Bolivia).

## Instalar (una vez)

Antes: crear `/etc/bussinesms-backup/sql.env` (root, `600`) con `SQL_USER=` y `SQL_PASSWORD=` de un login con permiso de backup, y `/etc/bussinesms-backup/rclone.conf` (root, `600`) con los remotos `r2` y `r2cifrado`. Instalar `rclone` oficial.

```bash
# desde la PC, en la carpeta BackEnd/infra/backup
scp -P <PUERTO> bussinesms-backup.sh bussinesms-backup.service bussinesms-backup.timer administrator@<IP>:~

# en el servidor
sudo install -m 700 -o root -g root ~/bussinesms-backup.sh /usr/local/sbin/bussinesms-backup.sh
sudo install -m 644 ~/bussinesms-backup.service ~/bussinesms-backup.timer /etc/systemd/system/
sudo systemctl daemon-reload && sudo systemctl enable --now bussinesms-backup.timer
systemctl list-timers bussinesms-backup.timer
```

## Correrlo a mano

```bash
sudo systemctl start bussinesms-backup.service
sudo journalctl -u bussinesms-backup.service -n 30 --no-pager   # tiene que terminar con "BACKUP OK"
```

## Restaurar (siempre primero en una base aparte)

```bash
# 1. dejar el backup en el servidor (de /var/backups/bussinesms o bajado de R2 con rclone) y abrirlo
mkdir /tmp/restore && sudo tar -xzf backup-FECHA.tar.gz -C /tmp/restore
sudo cp /tmp/restore/BussinesMS_Navidad-FECHA.bak /var/opt/mssql/backup/ && sudo chown mssql:mssql /var/opt/mssql/backup/*.bak

# 2. ver los nombres logicos de los archivos del .bak
/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -Q "RESTORE FILELISTONLY FROM DISK = N'/var/opt/mssql/backup/BussinesMS_Navidad-FECHA.bak'"

# 3. restaurar con otro nombre (WITH MOVE usa los nombres logicos del paso anterior)
/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -b -Q "RESTORE DATABASE [BussinesMS_Navidad_Prueba] FROM DISK = N'/var/opt/mssql/backup/BussinesMS_Navidad-FECHA.bak' WITH MOVE 'BussinesMS_Navidad' TO '/var/opt/mssql/data/BussinesMS_Navidad_Prueba.mdf', MOVE 'BussinesMS_Navidad_log' TO '/var/opt/mssql/data/BussinesMS_Navidad_Prueba_log.ldf'"

# 4. comprobar (por ejemplo contar filas) y borrar la base de prueba
/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -Q "SELECT COUNT(*) FROM BussinesMS_Navidad_Prueba.dbo.Producto; DROP DATABASE BussinesMS_Navidad_Prueba"
```

Para restaurar de verdad sobre la base original: parar la API (`sudo systemctl stop bussinesms-api`), `RESTORE DATABASE [BussinesMS_Navidad] ... WITH REPLACE` y levantarla de nuevo.

Evolution: `docker exec -i bussinesms-evolution-postgres pg_restore -U <usuario> -d <base> --clean < evolution.dump`, y descomprimir `evolution-instances.tgz` en el volumen para recuperar la sesion de WhatsApp.
