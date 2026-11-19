#!/bin/sh
# Compile SALINLAHI's C# outside the Unity editor, with warnings as errors.
#
# Catches real errors before a merge, and is the evidence behind the report's
# claim that both script assemblies still build after the security changes.
#
#   Tools/compile_check.sh
#
# Needs Unity 6000.6.2f1 installed, and the main checkout's ScriptAssemblies
# (open the project in Unity once if they are missing). Writes nothing to the
# repository: all output goes to a temporary directory.

set -u

ROOT=$(cd "$(dirname "$0")/.." && pwd)

# WHERE THE EDITOR IS, on whichever machine this is run from. The project is
# worked on from a Mac and from a Windows box, and the two lay the same Unity
# out differently - everything the Mac keeps inside Unity.app/Contents the
# Windows build keeps under Editor/Data, and the standalone player DLLs are
# under a differently named module. The DLLs themselves are the same, so the
# only thing that varies is these five paths.
#
# PATHS GO TO csc AS THE PLATFORM WRITES THEM. The Windows Roslyn is a
# Windows program and does not understand the /d/... form a Git Bash gives
# it, so everything handed to it is run through `cygpath -m` there - which
# leaves paths alone everywhere else, because everywhere else there is no
# cygpath. That was good for an afternoon of "Metadata file could not be
# found" against files that were plainly on disk.
if [ -d /Applications ]; then
  UNITY="/Applications/Unity/Hub/Editor/6000.6.2f1"
  SCRIPTING="$UNITY/Unity.app/Contents/Resources/Scripting"
  DOTNET="$SCRIPTING/DotNetSdk/dotnet"
  PLAYER="$UNITY/PlaybackEngines/MacStandaloneSupport/Variations/mono/Managed"
  EDITORDLL="$SCRIPTING/Managed/UnityEngine"
  MAIN="$HOME/Documents/SALINLAHI"
  native() { printf '%s\n' "$1"; }
else
  UNITY=${UNITY_EDITOR:-"/d/Unity/6000.6.2f1/Editor"}
  SCRIPTING="$UNITY/Data"
  DOTNET="$SCRIPTING/DotNetSdk/dotnet.exe"
  PLAYER="$SCRIPTING/PlaybackEngines/windowsstandalonesupport/Variations/mono/Managed"
  EDITORDLL="$SCRIPTING/Managed/UnityEngine"
  MAIN="$ROOT"
  native() { cygpath -m "$1"; }
fi
CSC=$(ls "$SCRIPTING/DotNetSdk/sdk/"*/Roslyn/bincore/csc.dll 2>/dev/null | head -1)
BCL="$SCRIPTING/UnityReferenceAssemblies/unity-4.8-api"
ASM="$MAIN/Library/ScriptAssemblies"

for p in "$DOTNET" "$CSC" "$BCL" "$PLAYER" "$ASM"; do
  [ -e "$p" ] || { echo "compile_check: missing $p"; exit 2; }
done

WORK=$(mktemp -d)
trap 'rm -rf "$WORK"' EXIT

FLAGS="-target:library -nostdlib+ -noconfig -unsafe -langversion:9.0
-nowarn:0169,0414,0649,0436,0618,1701,1702 -warnaserror+"

refs() {
  for d in "$@"; do
    for f in "$d"/*.dll; do [ -e "$f" ] && echo "-r:$(native "$f")"; done
  done
}

# Every source file, in the form the compiler on this platform reads.
sources() {
  for f in $(find "$@" -name '*.cs'); do native "$f"; done
}

run() {
  label="$1"; rsp="$2"; log="$3"
  printf '%-44s' "$label"
  if "$DOTNET" "$CSC" "@$rsp" >"$log" 2>&1; then
    echo "-> exit 0"
  else
    echo "-> FAILED"
    sed -n '1,25p' "$log"
    FAILED=1
  fi
}

FAILED=0
echo "SALINLAHI compile check - Unity 6000.6.2f1, warnings as errors"
echo

# 1. Runtime scripts, against the player engine DLLs.
{
  echo "$FLAGS"
  echo "-out:$(native "$WORK/runtime.dll")"
  refs "$BCL" "$BCL/Facades" "$PLAYER"
  echo "-r:$(native "$ASM/UnityEngine.UI.dll")"
  echo "-r:$(native "$ASM/Unity.TextMeshPro.dll")"
  echo "-r:$(native "$ASM/SpeechToText.Runtime.dll")"
  sources "$ROOT/Assets/Scripts"
} > "$WORK/runtime.rsp"
run "Assets/Scripts/*.cs" "$WORK/runtime.rsp" "$WORK/runtime.log"

# 2. Runtime + editor scripts, against the editor engine DLLs.
{
  echo "$FLAGS"
  echo "-out:$(native "$WORK/editor.dll")"
  refs "$BCL" "$BCL/Facades" "$EDITORDLL"
  echo "-r:$(native "$ASM/UnityEngine.UI.dll")"
  echo "-r:$(native "$ASM/Unity.TextMeshPro.dll")"
  echo "-r:$(native "$ASM/SpeechToText.Runtime.dll")"
  sources "$ROOT/Assets/Scripts" "$ROOT/Assets/Editor"
} > "$WORK/editor.rsp"
run "Assets/Scripts/*.cs + Assets/Editor/*.cs" "$WORK/editor.rsp" "$WORK/editor.log"

echo
[ "$FAILED" = 0 ] && echo "Both assemblies compile clean." || echo "compile_check: a build failed."
exit "$FAILED"
