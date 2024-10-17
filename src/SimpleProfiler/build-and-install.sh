#!/bin/bash
#Build and install dlls necessary for profiling enter the Gungeon on Linux

GUNGEON_DIR="/xmedia/pretzel/Steam/steamapps/common/Enter the Gungeon"
PATCHER_DIR="${GUNGEON_DIR}/BepInEx/patchers"
PLUGIN_DIR="${GUNGEON_DIR}/BepInEx/plugins"

info() {
  echo -e "[>] $@"
}

d="$(pwd)"

info "Building MonoProfiler64.so"
cd "./MonoProfiler"
make clean
make
[ $? -gt 0 ] && echo "Something went wrong building MonoProfiler64.so" && exit
/bin/cp "./MonoProfiler64.so" "$GUNGEON_DIR/MonoProfiler64.so"
cd "$d"

info "Building MonoProfilerLoader.dll"
cd "./MonoProfilerLoader"
msbuild
[ $? -gt 0 ] && echo "Something went wrong building MonoProfilerLoader.dll" && exit
/bin/cp "./obj/Debug/MonoProfilerLoader.dll" "$PATCHER_DIR/MonoProfilerLoader.dll"
cd "$d"

info "Building MonoProfilerController.dll"
cd "./MonoProfilerController"
msbuild
[ $? -gt 0 ] && echo "Something went wrong building MonoProfilerController.dll" && exit
/bin/cp "./obj/Debug/MonoProfilerController.dll" "$PLUGIN_DIR/MonoProfilerController.dll"
cd "$d"

info "Done!"
