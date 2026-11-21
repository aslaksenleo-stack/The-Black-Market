#!/bin/bash
# Compiles the Unity code against Unity's API (reference assemblies from NuGet) and runs the
# edit-mode tests, without Unity. Needs the .NET 8 SDK. See Tools/README.md.
set -e
H="$(cd "$(dirname "$0")" && pwd)"
"$H/../fetch-refs.sh"
set +e
echo "== Runtime assembly"; dotnet build "$H/Runtime/Runtime.csproj" -nologo -v q 2>&1 | grep -E "error|warning CS|Build succeeded" | sed 's|\[/.*||' | sort -u
echo "== Editor assembly (NamedBuildTarget errors are false positives: the UnityEditor reference is 2021.1)"
dotnet build "$H/Editor/Editor.csproj" -nologo -v q 2>&1 | grep -E "error|Build succeeded" | grep -v NamedBuildTarget | sed 's|\[/.*||' | sort -u
echo "== Edit-mode tests (LookMath/TapDetector skipped: they execute UnityEngine maths, which is stubbed here)"
dotnet test "$H/Tests/Tests.csproj" -nologo --filter "FullyQualifiedName!~LookMathTests&FullyQualifiedName!~TapDetectorTests" 2>&1 \
  | grep -E "error|Failed |Passed!|Failed!|Expected|But was" | sed 's|\[/.*||' | head -60
