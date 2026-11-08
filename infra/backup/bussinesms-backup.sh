#!/bin/bash
# Backup de BussinesMS (corre como root en el VPS, via systemd timer):
#   - bases BussinesMS_Auth, BussinesMS_Sistema y BussinesMS_Navidad (SQL Server Express)
#   - base propia de Evolution (pg_dump dentro del contenedor de Postgres)
#   - sesion de WhatsApp (volumen de instancias de Evolution)
# Deja una copia en /var/backups/bussinesms (14 dias) y la sube CIFRADA a R2 (remoto r2cifrado:
# de /etc/bussinesms-backup/rclone.conf; el bucket borra solo los de mas de 30 dias).
#
# Credenciales de SQL Server: /etc/bussinesms-backup/sql.env (root, 600) con
#   SQL_USER=...
#   SQL_PASSWORD=...
# Nunca van en este script.
#
# SQL Server Express NO soporta WITH COMPRESSION: el .bak va sin comprimir y se comprime despues con gzip.
#
# Instalar en el servidor:
#   sudo install -m 700 -o root -g root ~/bussinesms-backup.sh /usr/local/sbin/bussinesms-backup.sh
set -euo pipefail
cd /tmp

FECHA=$(date +%Y%m%d-%H%M%S)
DIR=/var/backups/bussinesms
CONF=/etc/bussinesms-backup/rclone.conf
SQLENV=/etc/bussinesms-backup/sql.env
SQLCMD=/opt/mssql-tools18/bin/sqlcmd
BAKDIR=/var/opt/mssql/backup
BASES="BussinesMS_Auth BussinesMS_Sistema BussinesMS_Navidad"
CONTENEDOR_PG=bussinesms-evolution-postgres
VOLUMEN_EVOLUTION=bussinesms-evolution_evolution_instances
TMP="$DIR/.en-curso-$FECHA"

# Credenciales (sqlcmd lee la clave de SQLCMDPASSWORD: no queda en la lista de procesos)
set -a
# shellcheck disable=SC1090
. "$SQLENV"
set +a
export SQLCMDPASSWORD="$SQL_PASSWORD"

mkdir -p "$DIR"
chmod 700 "$DIR"
mkdir "$TMP"
# Al salir (bien o mal): borra la carpeta de trabajo y cualquier .bak de esta corrida que haya quedado en el directorio de SQL Server
trap 'rm -rf "$TMP"; find "$BAKDIR" -maxdepth 1 -name "*-$FECHA.bak" -delete' EXIT
install -d -o mssql -g mssql -m 750 "$BAKDIR"

echo "1/4 bases de SQL Server"
for BASE in $BASES; do
  "$SQLCMD" -C -S 127.0.0.1 -U "$SQL_USER" -b \
    -Q "BACKUP DATABASE [$BASE] TO DISK = N'$BAKDIR/$BASE-$FECHA.bak' WITH CHECKSUM, INIT, NAME = N'$BASE-$FECHA'"
  mv "$BAKDIR/$BASE-$FECHA.bak" "$TMP/"
done

echo "2/4 base de Evolution"
docker exec "$CONTENEDOR_PG" sh -c 'pg_dump -U "$POSTGRES_USER" -Fc "$POSTGRES_DB"' > "$TMP/evolution.dump"

echo "3/4 sesion de WhatsApp"
VOL=$(docker volume inspect -f '{{.Mountpoint}}' "$VOLUMEN_EVOLUTION")
tar -czf "$TMP/evolution-instances.tgz" -C "$VOL" .

ARCHIVO="$DIR/backup-$FECHA.tar.gz"
tar -czf "$ARCHIVO" -C "$TMP" .
chmod 600 "$ARCHIVO"

echo "4/4 subiendo a R2 (cifrado)"
rclone --config "$CONF" copy "$ARCHIVO" r2cifrado:

find "$DIR" -maxdepth 1 -name 'backup-*.tar.gz' -mtime +14 -delete

echo "BACKUP OK: backup-$FECHA.tar.gz ($(du -h "$ARCHIVO" | cut -f1))"
