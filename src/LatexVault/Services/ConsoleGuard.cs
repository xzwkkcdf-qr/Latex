using System.Runtime.InteropServices;

namespace LatexVault.Services;

/// <summary>
/// WinExe has no console; TeX tools are console apps and their children flash a black window.
/// Allocate one hidden console early so latexmk/xelatex inherit it instead of creating visible ones.
/// </summary>
internal static class ConsoleGuard
{
    private const int SwHide = 0;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleCtrlHandler(IntPtr handler, bool add);

    public static void Suppress()
    {
        try
        {
            AllocConsole();
            var hwnd = GetConsoleWindow();
            if (hwnd != IntPtr.Zero)
                ShowWindow(hwnd, SwHide);
            // Ignore Ctrl+C from the hidden console while compiling.
            SetConsoleCtrlHandler(IntPtr.Zero, true);
        }
        catch
        {
            // Best-effort only.
        }
    }
}
