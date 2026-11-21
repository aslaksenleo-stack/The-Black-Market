#!/bin/bash
# Renders the game's generated art outside Unity: characters, items and the shop from the player's eye.
# Needs .NET 8, Node and Playwright's Chromium. Output PNGs go to Tools/ArtPreview/out.
set -e
H="$(cd "$(dirname "$0")" && pwd)"; cd "$H"; mkdir -p out
[ -d node_modules/three ] || npm install --silent --no-save three@0.160.0
dotnet run -c Release -- out/models.json models >/dev/null
dotnet run -c Release --no-build -- out/shop.json shop >/dev/null
(python3 -m http.server 8765 >/dev/null 2>&1 &) ; sleep 1
node shot.js "http://localhost:8765/gallery.html?src=out/models.json&f=char_&cols=4&aspect=1.3" out/characters.png 1600 1000
node shot.js "http://localhost:8765/gallery.html?src=out/models.json&f=item_&cols=6&aspect=0.9&yaw=30" out/items.png 1600 1000
node shot.js "http://localhost:8765/shop.html?src=out/shop.json&yaw=0&pitch=10&fov=60" out/shop_front.png 1600 900
node shot.js "http://localhost:8765/shop.html?src=out/shop.json&yaw=-80&pitch=0&fov=60" out/shop_left.png 1600 900
node shot.js "http://localhost:8765/shop.html?src=out/shop.json&yaw=75&pitch=8&fov=60" out/shop_right.png 1600 900
echo "Wrote $H/out/*.png"
