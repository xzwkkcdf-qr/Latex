using System.Diagnostics;
using System.Text;
using LatexVault.Models;

namespace LatexVault.Services;

public sealed class CompileService
{
    private Process? _running;
    private readonly object _gate = new();

    public static string ResolvePdfPath(string texPath) =>
        Path.ChangeExtension(Path.GetFullPath(texPath), ".pdf");

    public static ProcessStartInfo BuildProcessStart(
        CompileEngine engine,
        string texPath,
        string? executableOverride = null)
    {
        texPath = Path.GetFullPath(texPath);
        var workDir = Path.GetDirectoryName(texPath)!;
        var fileName = Path.GetFileName(texPath);
        var (exe, args) = engine switch
        {
            CompileEngine.LatexMk => (executableOverride ?? "latexmk", $"-pdf -interaction=nonstopmode \"{fileName}\""),
            CompileEngine.XeLatex => (executableOverride ?? "xelatex", $"-interaction=nonstopmode \"{fileName}\""),
            CompileEngine.PdfLatex => (executableOverride ?? "pdflatex", $"-interaction=nonstopmode \"{fileName}\""),
            _ => throw new ArgumentOutOfRangeException(nameof(engine))
        };

        return new ProcessStartInfo
        {
            FileName = exe,
            Arguments = args,
            WorkingDirectory = workDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
    }

    public async Task<CompileResult> CompileAsync(
        CompileEngine engine,
        string texPath,
        AppSettings settings,
        CancellationToken ct = default)
    {
        var exe = engine switch
        {
            CompileEngine.LatexMk => settings.LatexMkPath,
            CompileEngine.XeLatex => settings.XeLatexPath,
            CompileEngine.PdfLatex => settings.PdfLatexPath,
            _ => null
        };

        ProcessStartInfo psi;
        try
        {
            psi = BuildProcessStart(engine, texPath, string.IsNullOrWhiteSpace(exe) ? null : exe);
        }
        catch (Exception ex)
        {
            return new CompileResult { Success = false, ErrorHint = ex.Message, Log = ex.Message };
        }

        CancelRunning();

        var log = new StringBuilder();
        try
        {
            var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            lock (_gate) { _running = proc; }
            proc.OutputDataReceived += (_, e) => { if (e.Data != null) lock (log) log.AppendLine(e.Data); };
            proc.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (log) log.AppendLine(e.Data); };

            if (!proc.Start())
                return new CompileResult { Success = false, ErrorHint = "Failed to start engine.", Log = "" };

            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            await proc.WaitForExitAsync(ct).ConfigureAwait(false);
            var text = log.ToString();
            var pdf = ResolvePdfPath(texPath);
            var ok = proc.ExitCode == 0 && File.Exists(pdf);
            return new CompileResult
            {
                Success = ok,
                ExitCode = proc.ExitCode,
                Log = text,
                PdfPath = ok ? pdf : (File.Exists(pdf) ? pdf : null),
                ErrorHint = ok ? null : "Compile failed. See log."
            };
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return new CompileResult
            {
                Success = false,
                ErrorHint = "Engine not found. Set path in Settings.",
                Log = log.ToString()
            };
        }
        finally
        {
            lock (_gate) { _running = null; }
        }
    }

    public void CancelRunning()
    {
        lock (_gate)
        {
            try
            {
                if (_running is { HasExited: false })
                {
                    _running.Kill(entireProcessTree: true);
                    _running.Dispose();
                }
            }
            catch { /* ignore */ }
            _running = null;
        }
    }
}
