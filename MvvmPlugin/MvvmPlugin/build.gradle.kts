import com.jetbrains.plugin.structure.base.utils.isFile
import groovy.ant.FileNameFinder
import org.apache.tools.ant.taskdefs.condition.Os
import org.gradle.process.ExecOperations
import org.jetbrains.intellij.platform.gradle.Constants
import org.jetbrains.intellij.platform.gradle.IntelliJPlatformType
import org.jetbrains.intellij.platform.gradle.TestFrameworkType
import org.jetbrains.intellij.platform.gradle.tasks.VerifyPluginTask
import org.jetbrains.kotlin.gradle.dsl.JvmTarget
import java.io.ByteArrayOutputStream
import javax.inject.Inject
import kotlin.io.path.absolutePathString
import kotlin.io.path.isDirectory

// To access libs values go to the libs.versions.toml file

plugins {
    id("java")
    alias(libs.plugins.kotlinJvm)
    alias(libs.plugins.gradleJvmWrapper)
    alias(libs.plugins.intelliJPlatform)    
}

abstract class BuildExecServices {
    @get:Inject
    abstract val execOperations: ExecOperations
}

val execOperations = objects.newInstance<BuildExecServices>().execOperations
val buildToolExecutable = objects.property<String>()
val buildToolArguments = objects.listProperty<String>()

val isWindows = Os.isFamily(Os.FAMILY_WINDOWS)
extra["isWindows"] = isWindows

val DotnetSolution: String by project
val BuildConfiguration: String by project
val ProductVersion: String by project
val DotnetPluginId: String by project
val RiderPluginId: String by project
val PublishToken: String by project
val dotNetSourceDirectory = file("src/dotnet")

val riderDotNetSdkPath by lazy {
    val path = intellijPlatform.platformPath.resolve("lib/DotNetSdkForRdPlugins")
    check(path.isDirectory()) { "Rider .NET SDK path does not exist: $path" }
    path.absolutePathString()
}

tasks.buildSearchableOptions {
    enabled = false
}

allprojects {
    repositories {
        maven { setUrl("https://cache-redirector.jetbrains.com/maven-central") }
    }
}

repositories {
    intellijPlatform {
        defaultRepositories()
        jetbrainsRuntime()
    }
}

tasks.wrapper {
    gradleVersion = "8.14.5"
    distributionType = Wrapper.DistributionType.ALL
    distributionUrl = "https://cache-redirector.jetbrains.com/services.gradle.org/distributions/gradle-${gradleVersion}-all.zip"
}

version = extra["PluginVersion"] as String

tasks.processResources {
    from("dependencies.json") { into("META-INF") }
}

sourceSets {
    main {
        java.srcDir("src/rider/main/java")
        kotlin.srcDir("src/rider/main/kotlin")
        resources.srcDir("src/rider/main/resources")
    }
}

tasks.compileKotlin {
    compilerOptions.jvmTarget.set(JvmTarget.JVM_17)
}

val setBuildTool by tasks.registering {
    doLast {
        buildToolExecutable.set("dotnet")
        var args = mutableListOf("msbuild")

        if (isWindows) {
            val stdout = ByteArrayOutputStream()
            execOperations.exec {
                executable("${rootDir}\\tools\\vswhere.exe")
                args("-latest", "-property", "installationPath", "-products", "*")
                standardOutput = stdout
                workingDir(rootDir)
            }

            val directory = stdout.toString().trim()
            if (directory.isNotEmpty()) {
                val files = FileNameFinder().getFileNames("${directory}\\MSBuild", "**/MSBuild.exe")
                buildToolExecutable.set(files.get(0))
                args = mutableListOf("/v:minimal")
            }
        }

        args.add("${DotnetSolution}")
        args.add("/p:Configuration=${BuildConfiguration}")
        args.add("/p:HostFullIdentifier=")
        buildToolArguments.set(args)
    }
}

val compileDotNet by tasks.registering {
    dependsOn(setBuildTool)
    doLast {
        val executable = buildToolExecutable.get()
        val arguments = buildToolArguments.get().toMutableList()
        arguments.add("/p:RestoreConfigFile=${dotNetSourceDirectory}/nuget.config")
        arguments.add("/t:Restore;Rebuild")
        execOperations.exec {
            executable(executable)
            args(arguments)
            workingDir(rootDir)
        }
    }
}

val testDotNet by tasks.registering {
    doLast {
        execOperations.exec {
            executable("dotnet")
            args("test", "${DotnetSolution}", "/p:RestoreConfigFile=${dotNetSourceDirectory}/nuget.config", "--logger", "GitHubActions")
            workingDir(rootDir)
        }
    }
}

tasks.buildPlugin {
    doLast {
        copy {
            from(layout.buildDirectory.file("distributions/${rootProject.name}-${version}.zip"))
            into("${rootDir}/output")
        }

        // TODO: See also org.jetbrains.changelog: https://github.com/JetBrains/gradle-changelog-plugin
        val changelogText = file("${rootDir}/CHANGELOG.md").readText()
        val changelogMatches = Regex("(?s)(-.+?)(?=##|$)").findAll(changelogText)
        val changeNotes = changelogMatches.map {
            it.groups[1]!!.value.replace("(?s)- ".toRegex(), "\u2022 ").replace("`", "").replace(",", "%2C").replace(";", "%3B")
        }.take(1).joinToString()

        val executable = buildToolExecutable.get()
        val arguments = buildToolArguments.get().toMutableList()
        arguments.add("/t:Pack")
        arguments.add("/p:PackageOutputPath=${rootDir}/output")
        arguments.add("/p:PackageReleaseNotes=${changeNotes}")
        arguments.add("/p:PackageVersion=${version}")
        execOperations.exec {
            executable(executable)
            args(arguments)
            workingDir(rootDir)
        }
    }
}

dependencies {
    intellijPlatform {
        rider(ProductVersion) {
            useInstaller = false
        }
        // Hosts the backend-defined settings UI (SimpleOptionsPage).
        bundledModule("intellij.rider.rdclient.dotnet")
        jetbrainsRuntime()
        pluginVerifier(libs.intellijPluginVerifierCli.map { it.version!! })
        testFramework(TestFrameworkType.Bundled)

        // TODO: add plugins
        // bundledPlugin("uml")
        // bundledPlugin("com.jetbrains.ChooseRuntime:1.0.9")
    }
}

val generateDotNetSdkProperties by tasks.registering {
    val outputFile = layout.buildDirectory.file("DotNetSdkPath.Generated.props")
    outputs.file(outputFile)
    doLast {
        outputFile.get().asFile.writeText(
            """
            <Project>
              <PropertyGroup>
                <DotNetSdkPath>$riderDotNetSdkPath</DotNetSdkPath>
              </PropertyGroup>
            </Project>
            """.trimIndent()
        )
    }
}

val generateNuGetConfig by tasks.registering {
    val outputFile = dotNetSourceDirectory.resolve("nuget.config")
    outputs.file(outputFile)
    doLast {
        outputFile.writeText(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <!-- Generated by Gradle. Run ./gradlew prepare to regenerate. -->
            <configuration>
              <packageSources>
                <add key="rider-sdk" value="$riderDotNetSdkPath" />
              </packageSources>
            </configuration>
            """.trimIndent()
        )
    }
}

tasks.register("prepare") {
    dependsOn(generateDotNetSdkProperties, generateNuGetConfig)
}

compileDotNet.configure {
    dependsOn(generateDotNetSdkProperties, generateNuGetConfig)
}

testDotNet.configure {
    dependsOn(generateDotNetSdkProperties, generateNuGetConfig)
}

intellijPlatform {
    pluginVerification {
        failureLevel.add(VerifyPluginTask.FailureLevel.DEPRECATED_API_USAGES)
        ides {
            create(IntelliJPlatformType.Rider, libs.versions.riderSdk) {
                useInstaller = false
            }
            create(IntelliJPlatformType.Rider, libs.versions.riderSdkPreview) {
                useInstaller = false
            }
        }
    }
}

tasks.runIde {
    // Match Rider's default heap size of 1.5Gb (default for runIde is 512Mb)
    maxHeapSize = "1500m"

    if (isWindows) {
        doFirst {
            // Gradle shortens long Windows classpaths into a manifest-only JAR,
            // which Rider's PathClassLoader cannot use. Let Java expand an argfile instead.
            val classpathArgs = temporaryDir.resolve("rider-classpath.args")
            classpathArgs.parentFile.mkdirs()
            val escapedClasspath = classpath.asPath.replace('\\', '/').replace("\"", "\\\"")
            classpathArgs.writeText("-classpath\n\"$escapedClasspath\"\n")
            classpath = files()
            jvmArgs("@${classpathArgs.absolutePath}")
        }
    }
}

tasks.patchPluginXml {
    // TODO: See also org.jetbrains.changelog: https://github.com/JetBrains/gradle-changelog-plugin
    val changelogText = file("${rootDir}/CHANGELOG.md").readText()
    val changelogMatches = Regex("(?s)(-.+?)(?=##|\$)").findAll(changelogText)

    changeNotes.set(changelogMatches.map {
        it.groups[1]!!.value.replace("(?s)\r?\n".toRegex(), "<br />\n")
    }.take(1).joinToString())
}

tasks.prepareSandbox {
    dependsOn(compileDotNet)

    val outputFolder = "${rootDir}/src/dotnet/${DotnetPluginId}/bin/${DotnetPluginId}.Rider/${BuildConfiguration}"
    val dllFiles = listOf(
            "$outputFolder/${DotnetPluginId}.dll",
            "$outputFolder/${DotnetPluginId}.pdb",

            // TODO: add additional assemblies
    )

    dllFiles.forEach({ f ->
        val file = file(f)
        from(file, { into("${rootProject.name}/dotnet") })
    })

    doLast {
        dllFiles.forEach({ f ->
            val file = file(f)
            if (!file.exists()) throw RuntimeException("File ${file} does not exist")
        })
    }
}

tasks.publishPlugin {
    dependsOn(testDotNet)
    dependsOn(tasks.buildPlugin)
    token.set("${PublishToken}")

    doLast {
        execOperations.exec {
            executable("dotnet")
            args("nuget","push","output/${DotnetPluginId}.${version}.nupkg","--api-key","${PublishToken}","--source","https://plugins.jetbrains.com")
            workingDir(rootDir)
        }
    }
}

tasks.check {
    dependsOn(tasks.verifyPlugin)
}

val testRiderPreview by intellijPlatformTesting.testIde.registering {
    version = libs.versions.riderSdkPreview
    useInstaller = false
    task {
        enabled = libs.versions.riderSdk.get() != libs.versions.riderSdkPreview.get()
    }
}

tasks.check {
    dependsOn(testRiderPreview)
}

val riderModel: Configuration by configurations.creating {
    isCanBeConsumed = true
    isCanBeResolved = false
}

artifacts {
    add(riderModel.name, provider {
        intellijPlatform.platformPath.resolve("lib/rd/rider-model.jar").also {
            check(it.isFile) {
                "rider-model.jar is not found at $riderModel"
            }
        }
    }) {
        builtBy(Constants.Tasks.INITIALIZE_INTELLIJ_PLATFORM_PLUGIN)
    }
}
