using System;
using System.IO;
using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
namespace smTablebases;



public partial class App : Application
{
    public static int[] Version = new int[4];
    public static string Version2 = string.Empty;
    public static string Version3 = string.Empty;
    public static string Version4 = string.Empty;
    public static bool ShowException = true;

    static App()
    {
        Version v = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);

        Version[0] = v.Major;
        Version[1] = v.Minor;
        Version[2] = v.Build;
        Version[3] = v.Revision;

        Version2 = $"{Version[0]}.{Version[1]}";
        Version3 = $"{Version2}.{Version[2]}";
        Version4 = $"{Version3}.{Version[3]}";
    }


    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();



        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = (Exception)e.ExceptionObject;

            if (ShowException)
            {
                ShowException = false;
                MsgBox.Show(e.ToString());
            }

        };

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime deskt)
        {
            deskt.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }




    public static string TmpFolder => Path.Combine(AppContext.BaseDirectory, "tmp");

    public static string DebugFolder => Path.Combine(AppContext.BaseDirectory, "Debug");

    public static string SettingsFile => Path.Combine(AppContext.BaseDirectory, "Settings.xml");

    public static string Md5AndOtherInfosFile => Path.Combine(AppContext.BaseDirectory, "TB.txt");


    public static string DbgTxtOutFile => Path.Combine(AppContext.BaseDirectory, "out.txt");



}
