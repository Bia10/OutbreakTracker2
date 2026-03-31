#!/usr/bin/env dotnet
// Task runner for OutbreakTracker2
// Usage: dotnet Build.cs <command> [args]

#:property PublishAot=false

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

var repoRoot = RepoRoot();
var cmd = args.Length > 0 ? args[0] : "help";

return cmd switch
{
    "build" => Build(args),
    "test" => Test(args),
    "run" => RunApp(args),
    "publish" => Publish(args),
    "bench" => Bench(args),
    "format" => Format(args),
    "clean" => Clean(),
    "help" or _ when cmd == "help" => Help(),
    _ => Help(),
};

int Build(string[] a)
{
    var config = a.Length > 1 ? a[1] : "Debug";
    Run("dotnet", $"build src/OutbreakTracker2.slnx -c {config}", repoRoot);
    return 0;
}

int Test(string[] a)
{
    var config = a.Length > 1 ? a[1] : "Debug";
    var filter = a.Length > 2 ? $" --filter \"{a[2]}\"" : "";
    Run("dotnet", $"test src/OutbreakTracker2.slnx -c {config} --verbosity normal{filter}", repoRoot);
    return 0;
}

int RunApp(string[] a)
{
    var appProject = Path.Combine(
        "src",
        "application",
        "OutbreakTracker2.Application",
        "OutbreakTracker2.Application.csproj"
    );
    var extraArgs = a.Length > 1 ? " -- " + string.Join(' ', a[1..]) : "";
    Run("dotnet", $"run --project {appProject}{extraArgs}", repoRoot);
    return 0;
}

int Publish(string[] a)
{
    var rid = a.Length > 1 ? a[1] : RuntimeInformation.RuntimeIdentifier;
    var config = a.Length > 2 ? a[2] : "Release";
    var appProject = Path.Combine(
        "src",
        "application",
        "OutbreakTracker2.Application",
        "OutbreakTracker2.Application.csproj"
    );
    var publishDir = Path.Combine(repoRoot, "publish", rid);
    Run("dotnet", $"publish {appProject} -c {config} -r {rid} -o \"{publishDir}\"", repoRoot);
    Console.WriteLine($"Published to: {publishDir}");
    return 0;
}

int Bench(string[] a)
{
    var filter = a.Length > 1 ? a[1] : "*";
    Run(
        "dotnet",
        $"run --project src/insights/OutbreakTracker2.Benchmarks/OutbreakTracker2.Benchmarks.csproj"
            + $" -c Release -- --filter \"{filter}\" --join",
        repoRoot
    );
    return 0;
}

int Format(string[] a)
{
    var verify = a.Length > 1 && a[1] == "check";
    Run("dotnet", "tool restore", repoRoot);
    Run("dotnet", verify ? "csharpier check ." : "csharpier format .", repoRoot);
    Run(
        "dotnet",
        verify
            ? "format style src/OutbreakTracker2.slnx --verify-no-changes"
            : "format style src/OutbreakTracker2.slnx",
        repoRoot
    );
    Run(
        "dotnet",
        verify
            ? "format analyzers src/OutbreakTracker2.slnx --verify-no-changes"
            : "format analyzers src/OutbreakTracker2.slnx",
        repoRoot
    );
    return 0;
}

int Clean()
{
    var artifacts = Path.Combine(repoRoot, "artifacts");
    if (Directory.Exists(artifacts))
    {
        Directory.Delete(artifacts, recursive: true);
        Console.WriteLine("Cleaned artifacts/");
    }
    var publish = Path.Combine(repoRoot, "publish");
    if (Directory.Exists(publish))
    {
        Directory.Delete(publish, recursive: true);
        Console.WriteLine("Cleaned publish/");
    }
    Run("dotnet", "clean src/OutbreakTracker2.slnx", repoRoot);
    return 0;
}

int Help()
{
    Console.WriteLine(
        """
        Usage: dotnet Build.cs <command> [args]

        Commands:
          build [config]              Build solution (default: Debug)
          test [config] [filter]      Run tests (default: Debug, all tests)
          run [-- app-args]           Run the application
          publish [rid] [config]      Publish self-contained app (default: current RID, Release)
          bench [filter]              Run BenchmarkDotNet benchmarks
          format [check]              Format C# (CSharpier) + code style; 'check' verifies only
          clean                       Delete artifacts/ and publish/ directories
          help                        Show this help
        """
    );
    return 0;
}

// ─── Helpers ─────────────────────────────────────────────────────────────────

static void Run(string exe, string arguments, string workingDir)
{
    var psi = new ProcessStartInfo(exe, arguments) { WorkingDirectory = workingDir, UseShellExecute = false };
    using var p = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start '{exe}'");
    p.WaitForExit();
    if (p.ExitCode != 0)
        throw new InvalidOperationException($"'{exe}' exited with code {p.ExitCode}");
}

static string RepoRoot([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;
