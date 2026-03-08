using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using smTablebases;

namespace smTablebases;

public partial class MsgBox : Window
{
    public MsgBox()
    {
        InitializeComponent();
    }
    
    public MsgBox(string title, string message)
    {
        InitializeComponent();
        Title = title;
        TextBoxMessage.Text = message;
    }

    private void OkClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
    
    private static int _showing;
    
    
    public static async Task ShowAsync(string message, string title = "Message")
    {
         if (Interlocked.Exchange(ref _showing, 1) == 1)
             return;
    
         try
         {
             if (Dispatcher.UIThread.CheckAccess())
             {
                 var owner = MainWindow.Instance;
                 var win = new MsgBox(title, message);
                 if (owner != null)
                     await win.ShowDialog(owner);
                 else
                     win.Show();
             }
             else
             {
                 await Dispatcher.UIThread.InvokeAsync(() =>
                 {
                     var owner = MainWindow.Instance;
                     var win = new MsgBox(title, message);
                     if (owner != null)
                         return win.ShowDialog(owner);
                     else
                     {
                         win.Show();
                         return Task.CompletedTask;
                     }
                 });
             }
         }
         finally
         {
             Interlocked.Exchange(ref _showing, 0);
         }
    }
    
    public static void Show(string message, string title = "Message")
    {
        _ = ShowAsync(message, title);
    }
}