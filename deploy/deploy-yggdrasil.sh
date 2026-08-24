#!/bin/sh
set -eu

[ "${IDUNN_ACTUATOR:-}" = 1 ] || { echo "Gjallar deployment belongs to Idunn." >&2; exit 77; }
[ "${IDUNN_COMMAND_AUTHORITY:-}" = idunn-daemon ] || { echo "Missing Idunn command authority." >&2; exit 77; }

source_commit="${IDUNN_SOURCE_COMMIT:-}"
request_id="${IDUNN_DEPLOYMENT_REQUEST_ID:-}"
case "$source_commit" in *[!0-9a-f]*|'') echo "Invalid Gjallar source commit." >&2; exit 1;; esac
[ "${#source_commit}" -eq 40 ] || { echo "Gjallar source commit must be 40 hex characters." >&2; exit 1; }
[ -n "$request_id" ] || { echo "Missing Idunn deployment request id." >&2; exit 1; }
case "$request_id" in *[!A-Za-z0-9._-]*) echo "Invalid Idunn deployment request id." >&2; exit 1;; esac

sdk_image="mcr.microsoft.com/dotnet/sdk@sha256:0e53453ccfc8ff2d51319fe80c678971c6d0f8008dff3565fa88e15840b69854"
build_root="/srv/gjallar/build/$request_id"
workspace="$build_root/workspace"
publish="$build_root/publish"
release="/srv/gjallar/releases/$source_commit"
current="/srv/gjallar/current"
state_root="/var/lib/gamecult/gjallar"

cleanup() { rm -rf -- "$build_root"; }
trap cleanup EXIT
[ ! -e "$build_root" ] || { echo "Gjallar build attempt already exists: $build_root" >&2; exit 1; }
install -d -o root -g root -m 0755 "$workspace" "$publish"

archive_repo() {
  name="$1"
  commit="$2"
  repo="/srv/build/$name"
  [ -d "$repo/.git" ] || { echo "Missing Idunn build mirror: $repo" >&2; exit 1; }
  sudo -H -u idunn git -C "$repo" fetch --prune origin
  sudo -H -u idunn git -C "$repo" cat-file -e "$commit^{commit}"
  install -d -o root -g root -m 0755 "$workspace/$name"
  git -C "$repo" archive "$commit" | tar -x -C "$workspace/$name"
}

archive_repo Gjallar "$source_commit"
cultmath_commit="$(tr -d '\r\n' < "$workspace/Gjallar/deploy/cultmath.commit")"
cultlib_commit="$(tr -d '\r\n' < "$workspace/Gjallar/deploy/cultlib.commit")"
eve_commit="$(tr -d '\r\n' < "$workspace/Gjallar/deploy/eve.commit")"
archive_repo CultMath "$cultmath_commit"
archive_repo CultLib "$cultlib_commit"
archive_repo Eve "$eve_commit"

docker run --rm \
  -v "$workspace:/workspace" \
  -v "$publish:/publish" \
  -w /workspace/Gjallar \
  "$sdk_image" \
  dotnet publish src/Gjallar/Gjallar.csproj -c Release -r linux-x64 --self-contained true -o /publish

docker run --rm --network none \
  -v "$publish:/app:ro" \
  -v "$build_root:/smoke" \
  "$sdk_image" \
  /app/Gjallar --headless --frames 1 --url ws://127.0.0.1:1/eve/deck --stats-path /smoke/status.json --cultcache-path /smoke/gjallar.service.cc
grep -Fq '"mode": "yggdrasil-composition-daemon"' "$build_root/status.json"
grep -Fq '"enabled": false' "$build_root/status.json"

if [ ! -d "$release" ]; then
  install -d -o root -g root -m 0755 "$release"
  cp -a "$publish/." "$release/"
  printf '%s\n' \
    'schema_version=gamecult.gjallar.deployment_manifest.v2' \
    "source_commit=$source_commit" \
    "cultmath_commit=$cultmath_commit" \
    "cultlib_commit=$cultlib_commit" \
    "eve_commit=$eve_commit" \
    "sdk_image=$sdk_image" \
    "idunn_deployment_request_id=$request_id" > "$release/deployment.env"
  chmod -R a-w "$release"
fi

previous="$(readlink -f "$current" 2>/dev/null || true)"
ln -sfn "$release" "$current.next"
mv -Tf "$current.next" "$current"
install -d -o gjallar -g gjallar -m 0750 "$state_root"
systemctl daemon-reload
if ! systemctl enable --now gjallar-yggdrasil.service || ! systemctl is-active --quiet gjallar-yggdrasil.service; then
  if [ -n "$previous" ] && [ -d "$previous" ]; then
    ln -sfn "$previous" "$current.next"
    mv -Tf "$current.next" "$current"
    systemctl restart gjallar-yggdrasil.service || true
  fi
  echo "Gjallar Yggdrasil activation failed." >&2
  exit 1
fi

install -d -o root -g root -m 0755 /srv/gjallar/deploy
cp "$release/deployment.env" /srv/gjallar/deploy/deployment.env
echo "Gjallar $source_commit is active on Yggdrasil without a framebuffer backend."
