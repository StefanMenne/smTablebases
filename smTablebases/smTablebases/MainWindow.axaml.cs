using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using TBacc;


namespace smTablebases;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private static MainWindow? instance;
    private bool isCalculating = false;
    private readonly string title;
    private string textBlockText = "", logTextBoxText = "";
    public new event PropertyChangedEventHandler? PropertyChanged;



    public MainWindow()
    {
        instance = this;
        InitializeComponent();
        DataContext = this;
        GridThreading.CreateThreadingGrid(GridForThreading);
        _ = new DispatcherTimer(new TimeSpan(0, 0, 0, 0, 100), DispatcherPriority.Normal, TimerCallback );

        title = "smTablebases " + App.Version2;


#if RELEASEFINAL
#if DEBUG
        title += "debug_releasefinal";             // should not occur
#else
        // final
#endif
#else
#if DEBUG
        title += "d";
#else
        title += "r";
#endif
#endif


#if RELEASEFINAL
		MenuItemDebug.IsVisible = false;
		StackPanelDebug.IsVisible = false;
        ButtonToKqk.IsVisible = false;
#endif
#if !DEBUG
        MenuItemVerifyLcCompression.IsVisible = false;
#endif

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            CheckBoxShutdown.IsVisible = false;

        Settings.LoadXml(App.SettingsFile);
        CheckBoxTrack.IsChecked = Settings.Track;
        TextBoxTrack.Text = Settings.TrackText;
        UpdateThreadCount();

        TbInfoList = Settings.TbInfo;

        if (Settings.StopType == StopType.Never)
            RadioButtonNever.IsChecked = true;
        else if (Settings.StopType == StopType.Stop4Men)
            RadioButtonStop4.IsChecked = true;
        else if (Settings.StopType == StopType.Stop5Men)
            RadioButtonStop5.IsChecked = true;
        else if (Settings.StopType == StopType.StopTb)
            RadioButtonStopTb.IsChecked = true;
        TextBoxRam.Text = Settings.MemoryMb.ToString( CultureInfo.InvariantCulture );
        TextBoxTmpFolder.Text = Settings.TmpFolder;
        MenuItemResIndexCountPrint.IsChecked = Settings.ShowResIndexCount;

        BringCurrentTbToView();

        TbInfoList = Settings.TbInfo;

        foreach (RecalcResults rr in Enum.GetValues<RecalcResults>()) {
            MenuItem item = new MenuItem { Header = rr.ToString(), Tag = rr, ToggleType = MenuItemToggleType.Radio };
            item.Click += MenuItemChooseRecalcRes_Click;
            MenuItemCompressRecalcRes.Items.Add(item);
        }

        MenuItemFile.Items.Insert(3, Settings.GetBoolMenuItem("Perform large object heap compaction after each TB", "LargeObjectHeapCompaction"));
        MenuItemFile.Items.Insert(4, Settings.GetIntMenuItem("Read Buffer Size", "ReadBuffer", "FileStream Buffer-Size in Bytes when reading compressed files", null, 4 * 1024, 16 * 1024, 64 * 1024, 256 * 1024, 1024 * 1024, 4 * 1024 * 1024, 16 * 1024 * 1024, 64 * 1024 * 1024));
        MenuItemFile.Items.Insert(5, Settings.GetIntMenuItem("Read/Write TMP Buffer Size", "ReadWriteTmpBuffer", "FileStream Buffer-Size when storing/reading temporary files", null, 4 * 1024, 16 * 1024, 64 * 1024, 256 * 1024, 1024 * 1024, 4 * 1024 * 1024, 16 * 1024 * 1024, 64 * 1024 * 1024));
        MenuItemFile.Items.Insert(6, Settings.GetIntMenuItem("Write Buffer Size", "WriteBuffer", "FileStream Buffer-Size in Bytes when creating compressed files", null, 4 * 1024, 16 * 1024, 64 * 1024, 256 * 1024, 1024 * 1024, 4 * 1024 * 1024, 16 * 1024 * 1024, 64 * 1024 * 1024));

        MenuItemDebugFile.Items.Add(Settings.GetBoolMenuItem("Save uncompressed blocks", "SaveUncompressedBlocks"));
        MenuItemDebugFile.Items.Add(Settings.GetBoolMenuItem("Save data chunks at MD5 verification", "SaveDataChunksAtMd5"));

        MenuItemLcSettings.Items.Add(Settings.GetIntMenuItem("Level", "LcLevel", "0 Slowest/HighestCompression ... 6 Fastest/LowestCompression", MenuItemText, 0, 1, 2, 3, 4, 5, 6));
        MenuItemLcSettings.Items.Add(Settings.GetIntMenuItem("Bytes per item", "LcBytesPerItem", "Literal size; all references/matches/etc are multiple of this item size", null, 1, 2));

        // only BytesPerItem = 1 works currently
        MenuItem miBytesPerItem = (MenuItem)MenuItemLcSettings.Items[^1]!;
        for (int i = 1; i < miBytesPerItem.Items.Count; i++)
            ((MenuItem)miBytesPerItem.Items[i]!).IsEnabled = false;


        MenuItemLcSettings.Items.Add(Settings.GetEnumMenuItem("Set of Length", "LcLengthSet", "Length to be used during compression", typeof(LC.LengthSet)));
        MenuItem miSetOfLength = (MenuItem)MenuItemLcSettings.Items[^1]!;
        // only the default length works currently
        for ( int i=1 ;i< miSetOfLength.Items.Count ; i++ )
            ((MenuItem)miSetOfLength.Items[i]!).IsEnabled = false;

        MenuItemLcSettings.Items.Add(Settings.GetIntMenuItem("Literal position bits", "LcLiteralPosBits", "Amount of positions in bits literals will be coded independent", null, 0, 1, 2));
        // only BytesPerItem = 1 works currently
        MenuItem miLiteralPosBits = (MenuItem)MenuItemLcSettings.Items[^1]!;
        for (int i = 1; i < miLiteralPosBits.Items.Count; i++)
            ((MenuItem)miLiteralPosBits.Items[i]!).IsEnabled = false;

        MenuItemLcSettings.Items.Add(Settings.GetIntMenuItem("Previous byte high bits", "LcPrevByteHighBits", "Amount of bits of previous coded byte (starting highest bit) that use independent Probability trees.", null, 0, 1, 2, 3, 4, 5, 6, 7, 8));

        foreach (CompressionLevel level in Enum.GetValues<CompressionLevel>())
        {
            MenuItem item = new MenuItem { Header = level.ToString(), Tag = level };
            item.Click += MenuItemChooseDeflateCompression_Click;
            item.ToggleType = MenuItemToggleType.Radio;
            MenuItemDeflateCompressionLevel.Items.Add(item);
        }


        if (Settings.WindowHeight > 10 && Settings.WindowWidth > 10)
        {
            var screen = Screens.Primary;
            if (screen != null)
            {
                var workingArea = screen.WorkingArea;
                if (Settings.WindowLeft >= workingArea.X &&
                    Settings.WindowTop >= workingArea.Y &&
                    Settings.WindowLeft + Settings.WindowWidth <= workingArea.Right &&
                    Settings.WindowTop + Settings.WindowHeight <= workingArea.Bottom)
                {
                    this.Height = Settings.WindowHeight;
                    this.Width = Settings.WindowWidth;
                    this.Position = new PixelPoint((int)Settings.WindowLeft, (int)Settings.WindowTop);
                    this.WindowState = Settings.WindowState;
                }
            }
        }

        for (int i = 0; i < CompressionTypeStrings.Count; i++)
        {
            CompressionType ct = CompressionTypeStrings.FromInt(i);
            MenuItem mi = new MenuItem { Tag = ct, ToggleType = MenuItemToggleType.Radio };

            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal };
            mi.Header = sp;

            Label label = new Label { Content = CompressionTypeStrings.Get(ct), Width = 120 };
            sp.Children.Add(label);
            label = new Label { Content = CompressionTypeStrings.GetDescription(ct) };
            sp.Children.Add(label);

            mi.Click += MenuItemChooseCompression_Click;
            MenuItemAdvancedCompressionSettings.Items.Add(mi);
        }

        Calc.TablebaseFinished += Calc_TablebaseFinished;
        Title = title;
        UpdateCalcNow();
    }

    public void SetTitle(string text)
    {
        Dispatcher.UIThread.Invoke((Action)(() => {
            if ( text.Length == 0)
                Title = title;
            else
                Title = text + " " + title;
        }));
    }

    public TbInfoList TbInfoList
    {
        get { return field; }
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(nameof(TbInfoList));
            }
        }
    }

    public static MainWindow Instance
    {
        get => instance ?? throw new InvalidOperationException("MainWindow not initialized yet");
    }

    void MenuItemChooseDeflateCompression_Click(object? sender, RoutedEventArgs e)
    {
        Settings.DeflateCompressionLevel = (System.IO.Compression.CompressionLevel)((MenuItem)sender!).Tag!;
    }

    void MenuItemChooseRecalcRes_Click(object? sender, RoutedEventArgs e)
    {
        Settings.CompressRecalculateRes = (RecalcResults)((MenuItem)sender!).Tag!;
    }


    void MenuItemChooseCompression_Click(object? sender, RoutedEventArgs e)
    {
        Settings.CompressionType = (CompressionType)((MenuItem)sender!).Tag!;
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void MainWindow_WindowClosing(object sender, Avalonia.Controls.WindowClosingEventArgs e)
    {
        if (isCalculating)
        {
            MsgBox.Show("Stop calculation first.");
            e.Cancel = true;
        }
        else
        {
            Settings.WindowHeight = this.Height;
            Settings.WindowWidth = this.Width;
            Settings.WindowTop = this.Position.Y;
            Settings.WindowLeft = this.Position.X;
            Settings.WindowState = this.WindowState;
            Threading.CloseApp();
        }
    }

    private void MainWindow_Closed(object sender, EventArgs e)
    {
        SaveSettings();
        TbInfoFileList.Save();
    }


    private void SaveSettings()
    {
        Settings.Track = CheckBoxTrack.IsChecked == true;
        Settings.TrackText = TextBoxTrack.Text!;
        if (RadioButtonNever.IsChecked == true)
            Settings.StopType = StopType.Never;
        else if (RadioButtonStop4.IsChecked == true)
            Settings.StopType = StopType.Stop4Men;
        else if (RadioButtonStop5.IsChecked == true)
            Settings.StopType = StopType.Stop5Men;
        else if (RadioButtonStopTb.IsChecked == true)
            Settings.StopType = StopType.StopTb;
        Settings.SaveXml(App.SettingsFile);
    }





    public string? GetTrackText()
    {
        return (CheckBoxTrack.IsChecked == true) ?  TextBoxTrack.Text : null;
    }


    private void Button_Click(object sender, RoutedEventArgs e)
    {
        UpdateCalcStop();
        Message.Clear();
        isCalculating = true;
        UpdateCalcNow();
        ButtonCalc.IsEnabled = ButtonToKqk.IsEnabled = DockPanelChangeTbInfoProperties.IsEnabled = false;

        Task.Run(async () =>
        {
            try
            {
                await Calc.GenAsync();
            }
            finally
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    ButtonCalc.IsEnabled = ButtonToKqk.IsEnabled = DockPanelChangeTbInfoProperties.IsEnabled = true;
                    isCalculating = false;
                    UpdateCalcStop();
                    if (CheckBoxShutdown.IsChecked == true)
                    {
                        Close();
                        Process.Start("shutdown", "/s /t 0");
                    }
                });
            }
        });
    }


    private void TimerCallback(object? sender, EventArgs ea)
    {
        if (!Threading.GetProgress(out long cur, out long max))
        {
            max = Progress.Max;
            cur = Progress.Value;
        }


        if (Progress.IsIndeterminate)
        {
            DockPanelProgress.Opacity = 1.0;
            ProgressBar.IsIndeterminate = true;
        }
        else if (cur < 0 || max < 1 || cur > max)
        {
            DockPanelProgress.Opacity = 0.0;
        }
        else
        {
            double percent = (100L * cur) / ((double)max);
            string percentString = percent.ToString("##0.000", CultureInfo.InvariantCulture) + " %";
            LabelProgress.Content = percentString;
            DockPanelProgress.Opacity = 1.0;
            ProgressBar.Value = percent;
            ProgressBar.IsIndeterminate = false;
        }

        GridThreading.Instance.Update();


        string s;
        lock (Message.MessagesStringBuilder)
        {
            s = Message.MessagesStringBuilder.ToString();
        }

        if (textBlockText != s)
        {
            TextBox.Text = textBlockText = s;
            var scrollViewer = TextBox.Parent as ScrollViewer;
            scrollViewer?.ScrollToEnd();
        }


        lock (Message.LogStringBuilder)
        {
            s = Message.LogStringBuilder.ToString();
        }

        if (logTextBoxText != s)
        {
            TextBoxLog.Text = logTextBoxText = s;
            TextBoxLog.CaretIndex = TextBoxLog.Text.Length;
        }

    }

    private static void UpdateCalcNow()
    {
        Settings.TbInfo.CalcNowIndex = Settings.TbIndex;
    }

    private void ButtonClear_Click(object sender, RoutedEventArgs e)
    {
        Message.Clear();
    }

    private static readonly string[] MenuItemText = ["0 (best compression)", "1 (default)", "2", "3", "4", "5", "6 (fastest)" ];

    private void BringCurrentTbToView()
    {
        ListBoxTb.SelectedIndex = Settings.TbIndex;
        ListBoxTb.ScrollIntoView(ListBoxTb.SelectedItem!);

    }

    private void UpdateCalcStop()
    {
        if (RadioButtonNever.IsChecked == true)
            Calc.Stop = StopType.Never;
        else if (RadioButtonStop4.IsChecked == true)
            Calc.Stop = StopType.Stop4Men;
        else if (RadioButtonStop5.IsChecked == true)
            Calc.Stop = StopType.Stop5Men;
        else if (RadioButtonStopTb.IsChecked == true)
            Calc.Stop = StopType.StopTb;
    }



    private void Calc_TablebaseFinished(object? sender, TbFinishedEventArgs e)
    {
        Dispatcher.UIThread.Invoke((Action)(() => {
            UpdateCalcNow();
            BringCurrentTbToView();
            if ( e.CalculationWillContinue )
                Message.Clear();
            SaveSettings();
        }));
        SetTitle("");
    }


    private void ButtonDecThread_Click(object sender, RoutedEventArgs e)
    {
        Settings.ThreadCount = Math.Max(1, Settings.ThreadCount - 1);
        UpdateThreadCount();
    }

    private void ButtonIncThread_Click(object sender, RoutedEventArgs e)
    {
        Settings.ThreadCount = Math.Min(99, Settings.ThreadCount + 1);
        UpdateThreadCount();
    }

    private void UpdateThreadCount()
    {
        TextBlockThreadCount.Text = Settings.ThreadCount.ToString(CultureInfo.InvariantCulture);
        TextBoxThreadCountCompression.Text = Settings.ThreadCountCompression.ToString(CultureInfo.InvariantCulture);
    }

    private void MenuItemGC_Click(object sender, RoutedEventArgs e)
    {
        GC.Collect();
    }


    private void MenuItemTest_Click(object sender, RoutedEventArgs e)
    {
    }


    private void CheckBoxPause_IsCheckedChanged(object sender, RoutedEventArgs e)
    {
        if (CheckBoxPause.IsChecked == true)
            Calc.Pause = true;
        else
            Calc.Pause = false;
    }

    private void MenuItemExit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void MenuItemAbout_Click(object sender, RoutedEventArgs e)
    {
        MsgBox.Show("smTablebases V" + App.Version2, "smTablebases V" + App.Version2 );
    }


    private void ButtonCalcNow_Click(object sender, RoutedEventArgs e)
    {
        if ( ListBoxTb.SelectedItem is TbInfo info)  {
            Settings.TbIndex = Settings.TbInfo.CalcNowIndex = info.PiecesIndex - 1;
        }
    }

    private void RadioButtonStopTb_IsCheckedChanged(object sender, RoutedEventArgs e)
    {
        if (RadioButtonStopTb.IsChecked == true)
            Calc.Stop = StopType.StopTb;
    }

    private void RadioButtonStop4_IsCheckedChanged(object sender, RoutedEventArgs e)
    {
        if (RadioButtonStop4.IsChecked == true)
            Calc.Stop = StopType.Stop4Men;
    }

    private void RadioButtonStop5_IsCheckedChanged(object sender, RoutedEventArgs e)
    {
        if (RadioButtonStop5.IsChecked == true)
            Calc.Stop = StopType.Stop5Men;
    }

    private void RadioButtonNever_IsCheckedChanged(object sender, RoutedEventArgs e)
    {
        if (RadioButtonNever.IsChecked == true)
            Calc.Stop = StopType.Never;
    }


    private void TextBoxRam_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (int.TryParse(TextBoxRam.Text, out int mem) && Settings.IsInitialized)
            Settings.MemoryMb = mem;
    }

    private async void MenuItemMemoryUsage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
#if !RELEASEFINAL
            if (isCalculating)
                return;
            Message.Clear();
            isCalculating = true;
            ButtonCalc.IsEnabled = ButtonToKqk.IsEnabled = DockPanelChangeTbInfoProperties.IsEnabled = false;
            await Task.Run(MemoryUsage.Show);
            ButtonCalc.IsEnabled = ButtonToKqk.IsEnabled = DockPanelChangeTbInfoProperties.IsEnabled = true;
            isCalculating = false;
#endif
        }
        catch (Exception ex)
        {
            await MsgBox.ShowAsync(ex.Message);
        }
    }


    private void MenuItemShowSeveralInfos_Click(object sender, RoutedEventArgs e)
    {
#if !RELEASEFINAL
        Message.AddLogLine("Load chunk count from TaBasesWrite/TaBasesRead " + TaBasesWrite.ReadChunkCounter.ToString("###,##0", CultureInfo.InvariantCulture) + " / " + TaBasesRead.LoadChunkCounter.ToString("###,##0", CultureInfo.InvariantCulture));
        Message.AddLogLine(PieceGroupIndexTables.GetPrecalcInfo());
#endif
    }

    private void ButtonToKqk_Click(object sender, RoutedEventArgs e)
    {
        Settings.TbIndex = Settings.TbInfo.CalcNowIndex = 0;
        UpdateCalcNow();
        BringCurrentTbToView();
    }


    private async void ButtonSelectTmpFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            TopLevel topLevel = TopLevel.GetTopLevel(this)!;
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select tmp folder",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                TextBoxTmpFolder.Text = folders[0].Path.LocalPath;
                Settings.TmpFolder = folders[0].Path.LocalPath;
            }
        }
        catch (Exception ex)
        {
            await MsgBox.ShowAsync(ex.ToString());
        }
    }

    private void MenuItemCompression_SubmenuOpened(object sender, RoutedEventArgs e)
    {
        MenuItemCompressionProfileHigh.IsChecked = CompressionProfile.IsHigh;
        MenuItemCompressionProfileMedium.IsChecked = CompressionProfile.IsMedium;
        MenuItemCompressionProfileLow.IsChecked = CompressionProfile.IsLow;
    }


    private void ButtonGetRes_Click(object sender, RoutedEventArgs e)
    {
#if DEBUG
        Res r;

        if (TextBoxTrack!.Text!.Contains('/'))
            r = TBaccess.GetResult(TextBoxTrack.Text);
        else
        {
            UserPos up = Debug.ParsePos(TextBoxTrack.Text);
            r = TBaccess.GetResult(up.Pieces, up.Wk, up.Bk, up.Wtm, up.Fields, up.EpCapDst);
        }
        Message.Line(r.ToString());
#endif
    }

    private void ButtonIncThread_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint((Visual)sender).Properties.IsRightButtonPressed)
        {
            Settings.ThreadCount = Math.Min(99, Settings.ThreadCount + 10);
            UpdateThreadCount();
        }
    }

    private void ButtonDecThread_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint((Visual)sender).Properties.IsRightButtonPressed)
        {
            Settings.ThreadCount = Math.Max(1, Settings.ThreadCount - 10);
            UpdateThreadCount();
        }
    }

    private void MenuItemResIndexCountPrint_Click(object sender, RoutedEventArgs e)
    {
        MenuItem mi = (MenuItem)sender;
        Settings.ShowResIndexCount = mi.IsChecked;
    }


    private void MenuItemTxtOut_Click(object sender, RoutedEventArgs e)
    {
        Settings.TbInfo.WriteTextFile(App.DbgTxtOutFile);
        MsgBox.Show("File written:\r\n" + App.DbgTxtOutFile);
    }


    private void MenuItemAutoSettings_SubmenuOpened(object sender, RoutedEventArgs e)
    {
        MenuItem menuItem = (MenuItem)sender;
        string settingName = (string)menuItem.Tag!;
        PropertyInfo propertyInfo = typeof(Settings).GetProperty(settingName)!;
        Type propertyType = propertyInfo.PropertyType;

        if (propertyType == typeof(int))
        {
            int val = (int)propertyInfo.GetValue(null, null)!;

            foreach (MenuItem mi in menuItem.Items!)
            {
                int curVal = int.Parse((string)(mi!.Tag!), CultureInfo.InvariantCulture);
                mi.IsChecked = curVal == val;
            }
        }
        else if (propertyType == typeof(string))
        {
            string val = (string)propertyInfo.GetValue(null, null)!;

            foreach (MenuItem mi in menuItem.Items!)
            {
                string curVal = (string)mi!.Tag!;
                mi.IsChecked = curVal == val;
            }
        }
        else if (propertyType.IsEnum)
        {
            int val = (int)propertyInfo.GetValue(null, null)!;

            foreach (MenuItem mi in menuItem.Items!)
            {
                int curVal = (int)Enum.Parse(propertyType, (string)mi!.Tag!, ignoreCase: true);
                mi.IsChecked = (curVal == val);
            }
        }
        else throw new InvalidOperationException();
    }


    private void MenuItemAutoSettings_Click(object sender, RoutedEventArgs e)
    {
        MenuItem menuItem = (MenuItem)sender;
        MenuItem parentMenuItem = (MenuItem)menuItem.Parent!;
        string settingName = (string)parentMenuItem.Tag!;
        PropertyInfo propertyInfo = typeof(Settings).GetProperty(settingName)!;
        Type propertyType = propertyInfo.PropertyType;
        if (propertyType == typeof(int))
        {
            int curVal = int.Parse((string)menuItem.Tag!, CultureInfo.InvariantCulture);
            propertyInfo.SetValue(null, curVal, null);
        }
        else if (propertyType == typeof(string))
        {
            string curVal = (string)menuItem.Tag!;
            propertyInfo.SetValue(null, curVal, null);
        }
        else if (propertyType.IsEnum)
        {
            int curVal = (int)Enum.Parse(propertyType, (string)menuItem.Tag!, ignoreCase: true);
            propertyInfo.SetValue(null, curVal, null);
        }
        else throw new InvalidOperationException();
    }



    private void MenuItemKill_Click(object sender, RoutedEventArgs e)
    {
        Process.GetCurrentProcess().Kill();
    }


    private async void ButtonPieceGroupReorderWtm_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ListBoxTb.SelectedItem is TbInfo info)
            {
                Pieces pieces = Pieces.FromIndex(info.PiecesIndex);
                SelectTextWindow win = new SelectTextWindow(PieceGroupReorder.GetAllStrings(pieces))
                {
                    Title = "Piece Order (WTM)", SelectedString = info.PieceGroupReorderWtm
                };
                bool? result = await win.ShowDialog<bool?>(this);
                if (result == true)
                {
                    info.PieceGroupReorderWtm = win.SelectedString;
                }
            }
        }
        catch (Exception ex)
        {
            await MsgBox.ShowAsync(ex.Message);
        }
    }


    private async void ButtonPieceGroupReorderBtm_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ListBoxTb.SelectedItem is TbInfo info)
            {
                Pieces pieces = Pieces.FromIndex(info.PiecesIndex);
                SelectTextWindow win = new SelectTextWindow(PieceGroupReorder.GetAllStrings(pieces))
                {
                    Title = "Piece Order (BTM)", SelectedString = info.PieceGroupReorderBtm
                };
                if ( (await win.ShowDialog<bool?>(this)) == true)
                {
                    info.PieceGroupReorderBtm = win.SelectedString;
                }
            }
        }
        catch (Exception ex)
        {
            await MsgBox.ShowAsync(ex.Message);
        }
    }



    private void ButtonDecreaseCompressionThreads_Click(object sender, RoutedEventArgs e)
    {
        Settings.ThreadCountCompression = Math.Max(1, Settings.ThreadCountCompression - 1);
        UpdateThreadCount();
    }

    private void ButtonIncreaseCompressionThreads_Click(object sender, RoutedEventArgs e)
    {
        Settings.ThreadCountCompression = Math.Min(99, Settings.ThreadCountCompression + 1);
        UpdateThreadCount();
    }

    private void ButtonDecreaseCompressionThreads_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint((Visual)sender).Properties.IsRightButtonPressed)
        {
            Settings.ThreadCountCompression = Math.Max(1, Settings.ThreadCountCompression - 10);
            UpdateThreadCount();
        }
    }

    private void ButtonIncreaseCompressionThreads_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint((Visual)sender).Properties.IsRightButtonPressed)
        {
            Settings.ThreadCountCompression = Math.Min(99, Settings.ThreadCountCompression + 10);
            UpdateThreadCount();
        }
    }


    private void MenuItemDeflateCompressionLevel_SubmenuOpened(object sender, RoutedEventArgs e)
    {
        foreach (MenuItem? item in MenuItemDeflateCompressionLevel.Items)
        {
            if ( item is { } menuItem && menuItem.Tag! is CompressionLevel level)
            {
                item.IsChecked = (level == Settings.DeflateCompressionLevel);
            }
        }
    }


    private void MenuItemBrotliQuality_SubmenuOpened(object sender, RoutedEventArgs e)
    {
        foreach (MenuItem item in MenuItemBrotliQuality.Items!)
        {
            if (item is not null && item.Tag is int quality)
            {
                item.IsChecked = (quality == Settings.BrotliQuality);
            }
        }
    }

    private void MenuItemBrotliQuality_Click(object sender, RoutedEventArgs e)
    {
        Settings.BrotliQuality = (int)((MenuItem)sender).Tag!;
    }

    private void MenuItemCompressRecalcRes_SubmenuOpened(object sender, RoutedEventArgs e)
    {
        foreach (MenuItem item in MenuItemCompressRecalcRes.Items!)
        {
            if (item is not null && item.Tag! is RecalcResults rr)
            {
                item.IsChecked = (rr == Settings.CompressRecalculateRes);
            }
        }
    }


    private void MenuItemPrintCount_Click(object sender, RoutedEventArgs e)
    {
        MenuItem mi = (MenuItem)sender;
        MyTaskMv.PrintInfos = mi.IsChecked;
    }

    private void ButtonClearTmpFolder_OnClick(object? sender, RoutedEventArgs e)
    {
         TextBoxTmpFolder.Text = Settings.TmpFolder = "";
    }

    private void MenuItemAdvancedCompressionSettings_SubmenuOpened(object? sender, RoutedEventArgs e)
    {
        foreach (Control c in MenuItemAdvancedCompressionSettings.Items!)
        {
            // use Control instead of MenuItem due Separator
            if ( c is { Tag: CompressionType })
            {
                MenuItem mi = (c as MenuItem)!;
                mi.IsChecked = (((CompressionType)mi.Tag!) == Settings.CompressionType);
            }
        }
    }

    private void MenuItemCompressionProfileHigh_OnClick(object? sender, RoutedEventArgs e)
    {
        CompressionProfile.SetHigh();
    }

    private void MenuItemCompressionProfileMedium_OnClick(object? sender, RoutedEventArgs e)
    {
        CompressionProfile.SetMedium();
    }

    private void MenuItemCompressionProfileLow_OnClick(object? sender, RoutedEventArgs e)
    {
        CompressionProfile.SetLow();
    }

    private void MenuItemVerifyLcCompression_Click(object? sender, RoutedEventArgs e)
    {
#if DEBUG
        MenuItem mi = (MenuItem)sender!;
        LC.Coder.Verify = mi.IsChecked;
#endif
    }

}

