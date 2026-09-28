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
            CompileEngine.LatexMk => (executableOverride ?? "latexmk.exe", $"-pdf -interaction=nonstopmode -silent \"{fileName}\""),
            CompileEngine.XeLatex => (executableOverride ?? "xelatex.exe", $"-interaction=nonstopmode \"{fileName}\""),
            CompileEngine.PdfLatex => (executableOverride ?? "pdflatex.exe", $"-interaction=nonstopmode \"{fileName}\""),
            _ => throw new ArgumentOutOfRangeException(nameof(engine))
        };

        exe = NormalizeEnginePath(exe);

        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = args,
            WorkingDirectory = workDir,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            ErrorDialog = false,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        psi.Environment["MIKTEX_DISABLE_MAINTENANCE"] = "1";
        psi.Environment["MIKTEX_AUTOINSTALL"] = "0";
        return psi;
    }

    public static string NormalizeEnginePath(string exe)
    {
        if (string.IsNullOrWhiteSpace(exe))
            return exe;

        exe = exe.Trim().Trim('"');
        if (exe.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) ||
            exe.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
        {
            var sibling = Path.ChangeExtension(exe, ".exe");
            if (File.Exists(sibling))
                return sibling;
        }

        if (!exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
            !exe.Contains(Path.DirectorySeparatorChar) &&
            !exe.Contains(Path.AltDirectorySeparatorChar))
        {
            return exe + ".exe";
        }

        return exe;
    }

    public async Task<CompileResult> CompileAsync(
        CompileEngine engine,
        string texPath,
        AppSettings settings,
        CancellationToken ct = default)
    {
        var configured = engine switch
        {
            CompileEngine.LatexMk => settings.LatexMkPath,
            CompileEngine.XeLatex => settings.XeLatexPath,
            CompileEngine.PdfLatex => settings.PdfLatexPath,
            _ => null
        };

        var exe = string.IsNullOrWhiteSpace(configured)
            ? engine switch
            {
                CompileEngine.LatexMk => EngineLocator.FindExecutable("latexmk.exe", "latexmk"),
                CompileEngine.XeLatex => EngineLocator.FindExecutable("xelatex.exe", "xelatex"),
                CompileEngine.PdfLatex => EngineLocator.FindExecutable("pdflatex.exe", "pdflatex"),
                _ => null
            }
            : configured;

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
