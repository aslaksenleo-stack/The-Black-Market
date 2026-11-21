#!/bin/bash
# Downloads Unity reference assemblies (API only, no engine code) from nuget.org into Tools/.cache.
set -e
D="$(cd "$(dirname "$0")" && pwd)/.cache/refs"
get() { # id version
  [ -d "$D/$1" ] && return 0
  mkdir -p "$D/$1"; curl -sL -o "$D/$1.nupkg" "https://api.nuget.org/v3-flatcontainer/$1/$2/$1.$2.nupkg"
  (cd "$D/$1" && unzip -qo "../$1.nupkg") && rm "$D/$1.nupkg"
}
get unityengine.modules 2021.3.33
get unity3d.unityengine.ui 2020.3.21
get unity3d.sdk 2021.1.14.1
