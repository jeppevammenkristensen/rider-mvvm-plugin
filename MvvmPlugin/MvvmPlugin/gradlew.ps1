# Copyright 2015 the original author or authors.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#      https://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
#
# SPDX-License-Identifier: Apache-2.0

#requires -Version 7.3

# Windows PowerShell launcher equivalent to gradlew.bat, using PowerShell 7.3+.
# Usage: .\gradlew.ps1 --no-daemon help
# No param block: all arguments, including Gradle's switches, go to Gradle.
$gradleArguments = $args
$ErrorActionPreference = 'Stop'
$PSNativeCommandArgumentPassing = 'Standard'
$PSNativeCommandUseErrorActionPreference = $false
$originalJavaHome = $env:JAVA_HOME
$exitCode = 1

function Split-JvmOptions([string] $Options) {
    # Like the batch launcher, accept whitespace-separated options with double
    # quotes around values containing spaces. Never evaluate environment strings.
    if ([string]::IsNullOrWhiteSpace($Options)) {
        return
    }

    if (($Options.ToCharArray() | Where-Object { $_ -eq '"' }).Count % 2 -ne 0) {
        throw 'Unmatched double quote in JAVA_OPTS or GRADLE_OPTS.'
    }

    foreach ($match in [regex]::Matches($Options, '(?:[^\s"]+|"[^"]*")+')) {
        $match.Value.Replace('"', '')
    }
}

try {
    if (-not $IsWindows) {
        throw 'This launcher requires Windows. Use ./gradlew on macOS or Linux.'
    }

    # Keep these values aligned with the JVM wrapper block in gradlew.bat.
    $buildDirectory = Join-Path $env:LOCALAPPDATA 'gradle-jvm'
    $jvmDirectory = Join-Path $buildDirectory 'jdk-17.0.3.1_windows-x64_bin-d6ede5'
    $jvmUrl = 'https://download.oracle.com/java/17/archive/jdk-17.0.3.1_windows-x64_bin.zip'
    $flagPath = Join-Path $jvmDirectory '.flag'
    $wrapperJar = Join-Path $PSScriptRoot 'gradle/wrapper/gradle-wrapper.jar'

    if (-not (Test-Path -LiteralPath $wrapperJar -PathType Leaf)) {
        throw "Gradle wrapper JAR not found: $wrapperJar"
    }

    $cached = (Test-Path -LiteralPath $flagPath -PathType Leaf) -and
        ((Get-Content -LiteralPath $flagPath -Raw).Trim() -eq $jvmUrl)

    if (-not $cached) {
        New-Item -ItemType Directory -Path $buildDirectory -Force | Out-Null
        $archivePath = Join-Path $buildDirectory ('gradle-jvm-{0}.zip' -f [guid]::NewGuid())
        try {
            Write-Host "Downloading $jvmUrl to $archivePath"
            Invoke-WebRequest -Uri $jvmUrl -OutFile $archivePath

            if (Test-Path -LiteralPath $jvmDirectory) {
                Remove-Item -LiteralPath $jvmDirectory -Recurse -Force
            }
            New-Item -ItemType Directory -Path $jvmDirectory -Force | Out-Null
            Write-Host "Extracting $archivePath to $jvmDirectory"
            Expand-Archive -LiteralPath $archivePath -DestinationPath $jvmDirectory
            Set-Content -LiteralPath $flagPath -Value $jvmUrl -Encoding ascii
        }
        finally {
            if (Test-Path -LiteralPath $archivePath) {
                Remove-Item -LiteralPath $archivePath -Force
            }
        }
    }

    $javaHome = Get-ChildItem -LiteralPath $jvmDirectory -Directory |
        Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'bin/java.exe') -PathType Leaf } |
        Select-Object -Last 1
    if ($null -eq $javaHome) {
        throw "Unable to find java.exe under $jvmDirectory"
    }

    $env:JAVA_HOME = $javaHome.FullName
    $javaExecutable = Join-Path $env:JAVA_HOME 'bin/java.exe'
    $javaArguments = @('-Xmx64m', '-Xms64m')
    $javaArguments += @(Split-JvmOptions $env:JAVA_OPTS)
    $javaArguments += @(Split-JvmOptions $env:GRADLE_OPTS)
    $javaArguments += '-Dorg.gradle.appname=gradlew', '-jar', $wrapperJar
    $javaArguments += $gradleArguments

    & $javaExecutable @javaArguments
    $exitCode = $LASTEXITCODE
}
catch {
    [Console]::Error.WriteLine("ERROR: {0}", $_.Exception.Message)
}
finally {
    # Match the batch launcher's setlocal behavior for the caller's environment.
    $env:JAVA_HOME = $originalJavaHome
}

exit $exitCode
