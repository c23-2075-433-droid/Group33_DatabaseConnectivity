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

UNITY="/Applications/Unity/Hub/Editor/6000.6.2f1"
MAIN="$HOME/Documents/SALINLAHI"
ROOT=$(cd "$(dirname "$0")/.." && pwd)

DOTNET="$UNITY/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet"
CSC=$(ls "$UNITY/Unity.app/Contents/Resources/Scripting/DotNetSdk/sdk/"*/Roslyn/bincore/csc.dll 2>/dev/null | head -1)
BCL="$UNITY/Unity.app/Contents/Resources/Scripting/UnityReferenceAssemblies/unity-4.8-api"
PLAYER="$UNITY/PlaybackEngines/MacStandaloneSupport/Variations/mono/Managed"
EDITORDLL="$UNITY/Unity.app/Contents/Resources/Scripting/Managed/UnityEngine"
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
    for f in "$d"/*.dll; do [ -e "$f" ] && echo "-r:$f"; done
  done
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
  echo "-out:$WORK/runtime.dll"
  refs "$BCL" "$BCL/Facades" "$PLAYER"
  echo "-r:$ASM/UnityEngine.UI.dll"
  echo "-r:$ASM/Unity.TextMeshPro.dll"
  echo "-r:$ASM/SpeechToText.Runtime.dll"
  find "$ROOT/Assets/Scripts" -name '*.cs'
} > "$WORK/runtime.rsp"
run "Assets/Scripts/*.cs" "$WORK/runtime.rsp" "$WORK/runtime.log"

# 2. Runtime + editor scripts, against the editor engine DLLs.
{
  echo "$FLAGS"
  echo "-out:$WORK/editor.dll"
  refs "$BCL" "$BCL/Facades" "$EDITORDLL"
  echo "-r:$ASM/UnityEngine.UI.dll"
  echo "-r:$ASM/Unity.TextMeshPro.dll"
  echo "-r:$ASM/SpeechToText.Runtime.dll"
  find "$ROOT/Assets/Scripts" "$ROOT/Assets/Editor" -name '*.cs'
} > "$WORK/editor.rsp"
run "Assets/Scripts/*.cs + Assets/Editor/*.cs" "$WORK/editor.rsp" "$WORK/editor.log"

echo
[ "$FAILED" = 0 ] && echo "Both assemblies compile clean." || echo "compile_check: a build failed."
exit "$FAILED"
