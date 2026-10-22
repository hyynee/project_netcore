using System.Diagnostics;

namespace ShopCore.UnitTests.Support;

public static class DockerAvailability
{
    private static readonly Lazy<bool> DockerAvailable =
        new(IsDockerAvailabilityCheck, LazyThreadSafetyMode.ExecutionAndPublication);

    public static string? EnvironmentSkipReason =>
        string.Equals(Environment.GetEnvironmentVariable("SHOPCORE_RUN_SQL_TESTS"), "true",
            StringComparison.OrdinalIgnoreCase)
            ? null
            : "Set SHOPCORE_RUN_SQL_TESTS=true and ensure Docker is available.";

    public static bool IsDockerAvailable() => DockerAvailable.Value;

    private static bool IsDockerAvailabilityCheck()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "info --format '{{.ServerVersion}}'",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process is null)
            {
                return false;
            }

            if (!process.WaitForExit(3_000))
            {
                process.Kill();
                return false;
            }

            return process.ExitCode == 0;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    public static bool IsAvailable => EnvironmentSkipReason is null && IsDockerAvailable();

    public static string SkipReason => EnvironmentSkipReason
        ?? "Docker daemon is unavailable; start Docker and rerun the SQL Server integration test.";
}
