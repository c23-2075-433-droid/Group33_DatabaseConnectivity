#!/bin/sh
# Runs the negative tests in Tools/securitytests/SecurityRuleTests.cs against
# the game's own validation rules, without opening Unity.
#
#   Tools/test_security_rules.sh
#
# It compiles Assets/Scripts/NicknameRules.cs - the real file the game ships -
# together with the test, using the .NET SDK that comes inside the Unity
# install, and runs it. Exit status 0 means every negative test passed.
#
# Unity is only needed for its bundled dotnet; nothing here opens the editor or
# touches the project's Library folder.

set -u
ROOT=$(cd "$(dirname "$0")/.." && pwd)
UNITY=${UNITY_PATH:-/Applications/Unity/Hub/Editor/6000.6.2f1}
DOTNET="$UNITY/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet"

if [ ! -x "$DOTNET" ]; then
  echo "test_security_rules: no dotnet at $DOTNET"
  echo "Set UNITY_PATH to the Unity version installed here, e.g."
  echo "  UNITY_PATH=/Applications/Unity/Hub/Editor/6000.6.2f1 $0"
  exit 2
fi

OUT=$(mktemp -d)
trap 'rm -rf "$OUT"' EXIT

cat > "$OUT/tests.csproj" <<'PROJ'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>disable</Nullable>
    <AssemblyName>SecurityRuleTests</AssemblyName>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <StartupObject>SecurityRuleTests</StartupObject>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$(GameRoot)/Assets/Scripts/NicknameRules.cs" />
    <Compile Include="$(GameRoot)/Tools/securitytests/SecurityRuleTests.cs" />
  </ItemGroup>
</Project>
PROJ

DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 \
  "$DOTNET" run --project "$OUT/tests.csproj" -p:GameRoot="$ROOT" -v quiet 2>&1
exit $?
