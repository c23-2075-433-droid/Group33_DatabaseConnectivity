#!/bin/sh
# Runs the stress-checking tests in Tools/stresstests/StressTests.cs against the
# game's own answer-checking code and Teacher Rachel's own recordings, without
# opening Unity.
#
#   Tools/test_stress_rules.sh            run the tests (exit 0 = all passed)
#   Tools/test_stress_rules.sh generate   re-measure the recordings and rewrite
#                                         all three stress reference files
#   Tools/test_stress_rules.sh labelled [DIR]
#                                         judge real labelled recordings (default
#                                         Recordings/Labelled) and report accuracy
#   Tools/test_stress_rules.sh file WORD a.wav [b.wav ...]
#                                         measure WAVs (e.g. the Editor's
#                                         Library/StressRecordings) as answers to WORD
#
# Uses DOTNET if set, then the dotnet on PATH, then the one inside a Unity
# install (UNITY_PATH).

set -u
ROOT=$(cd "$(dirname "$0")/.." && pwd)

if [ -n "${DOTNET:-}" ]; then :
elif command -v dotnet >/dev/null 2>&1 && dotnet --list-sdks 2>/dev/null | grep -q .; then DOTNET=dotnet
elif [ -x "/c/Program Files/dotnet/dotnet.exe" ]; then DOTNET="/c/Program Files/dotnet/dotnet.exe"
else
  UNITY=${UNITY_PATH:-/Applications/Unity/Hub/Editor/6000.6.2f1}
  DOTNET="$UNITY/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet"
fi

OUT=$(mktemp -d)
trap 'rm -rf "$OUT"' EXIT

cat > "$OUT/tests.csproj" <<'PROJ'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <RollForward>Major</RollForward>
    <Nullable>disable</Nullable>
    <AssemblyName>StressTests</AssemblyName>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <StartupObject>StressTests</StartupObject>
    <NoWarn>CS0162;CS0414</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$(GameRoot)/Assets/Scripts/PronunciationChecker.cs" />
    <Compile Include="$(GameRoot)/Assets/Scripts/WordVariants.cs" />
    <Compile Include="$(GameRoot)/Assets/Scripts/WordStress.cs" />
    <Compile Include="$(GameRoot)/Assets/Scripts/StressPattern.cs" />
    <Compile Include="$(GameRoot)/Assets/Scripts/StressAnalyzer.cs" />
    <Compile Include="$(GameRoot)/Assets/Scripts/StressReferences.cs" />
    <Compile Include="$(GameRoot)/Assets/Scripts/StressReferenceData.cs" />
    <Compile Include="$(GameRoot)/Assets/Scripts/AnswerJudge.cs" />
    <Compile Include="$(GameRoot)/Assets/Scripts/RawStressAnalyzer.cs" />
    <Compile Include="$(GameRoot)/Assets/Scripts/RawStressReferences.cs" />
    <Compile Include="$(GameRoot)/Assets/Scripts/RawStressReferenceData.cs" />
    <Compile Include="$(GameRoot)/Assets/Scripts/SpeechFeatures.cs" />
    <Compile Include="$(GameRoot)/Assets/Scripts/SyllableAligner.cs" />
    <Compile Include="$(GameRoot)/Assets/Scripts/PronunciationAssessor.cs" />
    <Compile Include="$(GameRoot)/Assets/Scripts/PronunciationReferences.cs" />
    <Compile Include="$(GameRoot)/Assets/Scripts/PronunciationReferenceData.cs" />
    <Compile Include="$(GameRoot)/Assets/Editor/WavLevelStream.cs" />
    <Compile Include="$(GameRoot)/Assets/Editor/StressReferenceBuilder.cs" />
    <Compile Include="$(GameRoot)/Assets/Editor/PronunciationReferenceBuilder.cs" />
    <Compile Include="$(GameRoot)/Tools/stresstests/StressTests.cs" />
  </ItemGroup>
</Project>
PROJ

WIN_ROOT=$ROOT
command -v cygpath >/dev/null 2>&1 && WIN_ROOT=$(cygpath -m "$ROOT")

MODE=${1:-test}
[ $# -gt 0 ] && shift
DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 \
  "$DOTNET" run -c Release --project "$OUT/tests.csproj" -p:GameRoot="$WIN_ROOT" -v quiet -- "$MODE" "$WIN_ROOT" "$@" 2>&1
exit $?
