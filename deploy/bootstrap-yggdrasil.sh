#!/bin/sh
set -eu

[ "$(id -u)" -eq 0 ] || { echo "Gjallar bootstrap requires root." >&2; exit 1; }
id idunn >/dev/null 2>&1 || { echo "Idunn must be installed first." >&2; exit 1; }
id gjallar >/dev/null 2>&1 || useradd --system --home-dir /var/lib/gamecult/gjallar --create-home --shell /usr/sbin/nologin gjallar

clone_for_idunn() {
  name="$1"
  origin="$2"
  path="/srv/build/$name"
  if [ ! -d "$path/.git" ]; then
    install -d -o idunn -g idunn -m 0755 "$path"
    sudo -H -u idunn git clone --filter=blob:none --no-checkout "$origin" "$path"
  fi
  [ "$(sudo -H -u idunn git -C "$path" remote get-url origin)" = "$origin" ] || { echo "$path has a foreign origin." >&2; exit 1; }
  [ -z "$(find "$path" -xdev ! -user idunn -print -quit)" ] || { echo "$path has foreign ownership." >&2; exit 1; }
}

clone_for_idunn Gjallar https://github.com/GameCult/Gjallar.git
clone_for_idunn CultLib https://github.com/GameCult/CultLib.git
clone_for_idunn Eve https://github.com/GameCult/Eve.git

install -d -o root -g root -m 0755 /srv/odin/deploy-manifests /srv/gjallar/releases /srv/gjallar/deploy
install -d -o gjallar -g gjallar -m 0750 /var/lib/gamecult/gjallar
install -o root -g root -m 0755 "$(dirname "$0")/deploy-yggdrasil.sh" /srv/odin/deploy-manifests/gjallar
install -o root -g root -m 0644 "$(dirname "$0")/../systemd/gjallar-yggdrasil.service" /etc/systemd/system/gjallar-yggdrasil.service
systemctl daemon-reload
systemctl disable --now gjallar-yggdrasil.service 2>/dev/null || true
echo "Gjallar Yggdrasil body admitted but stopped. Idunn owns first deployment and continuity."
