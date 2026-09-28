namespace LatexVault.Models;

public sealed class CompileResult
{
    public bool Success { get; init; }
    public int ExitCode { get; init; }
    public string Log { get; init; } = "";
    public string? PdfPath { get; init; }
    public string? ErrorHint { get; init; }
}
