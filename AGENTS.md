# Rider Plugin Version Updates

The Rider frontend and the ReSharper/Rider backend must target the same stable Rider release.

1. Query the JetBrains release metadata. Use the `release` value from:
   `https://www.jetbrains.com/intellij-repository/releases/com/jetbrains/intellij/rider/riderRD/maven-metadata.xml`.
   Do not use a `-SNAPSHOT` version as the production target.
2. Update the stable version consistently in:
   - `MvvmPlugin/MvvmPlugin/gradle.properties` (`ProductVersion`)
   - `MvvmPlugin/MvvmPlugin/gradle/libs.versions.toml` (`riderSdk`)
   - `MvvmPlugin/MvvmPlugin/src/dotnet/Plugin.props` (`SdkVersion`), while that file still owns the ReSharper SDK package version.
3. For early compatibility coverage, query the snapshots metadata at the matching `snapshots/.../riderRD/maven-metadata.xml` URL and set `riderSdkPreview` to the latest EAP snapshot for the *next* Rider release. It is a test-only target, never the production target.
4. Run from `MvvmPlugin/MvvmPlugin`:
   ```powershell
   .\gradlew.bat --no-daemon help
   .\gradlew.bat --no-daemon -PBuildConfiguration=Release buildPlugin check testDotNet
   ```
5. Resolve all compilation, test, and Plugin Verifier failures before publishing. The GitHub Actions workflow runs the same checks on Windows, macOS, and Linux.

When the build uses the generated Rider SDK configuration, run `./gradlew prepare` before building the .NET solution directly. The generated `src/dotnet/nuget.config` and `build/DotNetSdkPath.Generated.props` are local build artifacts and must not be committed.
