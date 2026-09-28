using System;
using System.Windows;
using LatexVault.Services;

namespace LatexVault;

public static class EntryPoint
{
    [STAThread]
    public static void Main()
    {
        ConsoleGuard.Suppress();
        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
