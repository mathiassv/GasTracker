#!/bin/sh
# Starts as root only to make the mounted data directory writable by the non-root `app` user
# (host bind mounts are often root-owned), then drops privileges before starting the app.
set -e

if [ "$(id -u)" = "0" ]; then
    mkdir -p /app/data
    chown -R app:app /app/data
    exec setpriv --reuid=app --regid=app --init-groups "$@"
fi

exec "$@"
