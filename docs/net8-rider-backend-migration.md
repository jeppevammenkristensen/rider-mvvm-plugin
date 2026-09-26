# Rider Backend net8.0 Migration

## Recommendation

Move only the Rider backend to `net8.0`. Keep the ReSharper backend on `net472` because ReSharper plugin hosting still requires the .NET Framework target.

Do not target `net10.0` yet. The current Rider plugin template uses `net8.0`; revisit `net10.0` when JetBrains supports it in the template and generated Rider SDK packages.

## Plan

1. Preserve dual targeting.
   - Keep `ReSharperPlugin.MvvmPlugin.csproj` on `net472`.
   - Change only `ReSharperPlugin.MvvmPlugin.Rider.csproj` to `net8.0`.
   - Continue producing separate Rider and ReSharper assemblies.
2. Confirm Rider SDK support.
   - Run `./gradlew prepare`.
   - Confirm the generated `DotNetSdkForRdPlugins` configuration resolves the Rider SDK for `net8.0`.
3. Split target-specific dependencies.
   - Move .NET Framework-only references, including `System.Xaml`, behind a `net472` condition.
   - Retain `Microsoft.NETFramework.ReferenceAssemblies` only for `net472`.
   - Keep the Rider project on the generated Rider SDK package import.
4. Make source compatibility explicit.
   - Build the ReSharper project under `net472` and the Rider project under `net8.0`.
   - Use `#if RIDER` and `#if RESHARPER` only where host APIs differ.
   - Keep shared context-action logic host-neutral where possible.
5. Separate tests by host.
   - Keep existing ReSharper SDK tests on `net472`.
   - Add Rider-specific tests only where behavior differs after the target change.
6. Verify packaging.
   - Confirm `prepareSandbox` packages the Rider `net8.0` DLL and PDB.
   - Confirm the ReSharper NuGet package still contains the `net472` output.
   - Run `runIde` and exercise the context actions in the Rider sandbox.
7. Validate release gates.
   ```powershell
   .\gradlew.bat --no-daemon prepare
   .\gradlew.bat --no-daemon -PBuildConfiguration=Release buildPlugin check testDotNet
   ```
   - Resolve all compiler, test, and Plugin Verifier findings.
   - Confirm GitHub Actions passes on Windows, macOS, and Linux.

## net10.0 Reassessment

Consider `net10.0` only after all of the following are true:

- The official Rider plugin template targets it.
- The generated Rider SDK packages support it.
- CI installs .NET 10 on every platform.
- Raising the plugin development baseline is acceptable.
