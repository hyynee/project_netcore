using System.Diagnostics;

namespace ShopCore.UnitTests.Persistence;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("SHOPCORE_RUN_SQL_TESTS"), "true",
                StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Set SHOPCORE_RUN_SQL_TESTS=true and ensure Docker is available.";
            return;
        }

        if (!IsDockerAvailable())
        {
            Skip = "Docker daemon is unavailable; start Docker and rerun the SQL Server integration test.";
        }
    }

    private static bool IsDockerAvailable()
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
}
