using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;
using Microsoft.Win32;
using TopMemo2.Models;
using TopMemo2.Services;
using Forms = System.Windows.Forms;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using TabControl = System.Windows.Controls.TabControl;

namespace TopMemo2;

public sealed class MainWindow : Window
{
    private readonly SettingsStore _settingsStore = new();
    private readonly DispatcherTimer _mouseTimer = new();
    private readonly List<MemoTab> _tabs = [];
    private readonly TabControl _tabControl = new();
    private readonly TextBlock _statusText = new();
    private readonly Border _background = new();
    private Forms.NotifyIcon? _notifyIcon;
    private AppSettings _settings;
    private DateTime? _hideDueAt;
    private bool _isExiting;

    public MainWindow()
    {
        _settings = _settingsStore.Load();
        BuildUi();
        ApplySettings();
        LoadTabsFromSettings();

        _mouseTimer.Interval = TimeSpan.FromMilliseconds(75);
        _mouseTimer.Tick += OnMouseTimerTick;

        Closing += OnClosing;
    }

    public void StartHidden()
    {
        SetupTrayIcon();
        _mouseTimer.Start();
    }

    private void BuildUi()
    {
        Title = "TopMemo2";
        ShowInTaskbar = false;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Opacity = 1.0;
        Topmost = true;
        MinWidth = 320;
        MinHeight = 240;
        Resources.MergedDictionaries.Add(CreateTransparentChromeResources());

        _background.CornerRadius = new CornerRadius(8);
        _background.BorderThickness = new Thickness(1);
        _background.BorderBrush = new SolidColorBrush(Color.FromArgb(160, 120, 120, 120));
        Content = _background;

        var layout = new DockPanel();
        _background.Child = layout;

        var toolbar = new Grid
        {
            Height = 40,
            Margin = new Thickness(8, 6, 8, 0)
        };
        toolbar.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        };
        DockPanel.SetDock(toolbar, Dock.Top);
        layout.Children.Add(toolbar);

        var title = new TextBlock
        {
            Text = "TopMemo2",
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 12, 0)
        };
        toolbar.Children.Add(title);

        _statusText.Foreground = new SolidColorBrush(Color.FromRgb(230, 230, 230));
        _statusText.Margin = new Thickness(16, 0, 16, 8);
        _statusText.TextTrimming = TextTrimming.CharacterEllipsis;
        DockPanel.SetDock(_statusText, Dock.Bottom);
        layout.Children.Add(_statusText);

        _tabControl.Margin = new Thickness(8);
        _tabControl.Background = Brushes.Transparent;
        _tabControl.BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
        layout.Children.Add(_tabControl);
    }

    private void ApplySettings()
    {
        Left = _settings.Window.Left;
        Top = _settings.Window.Top;
        Width = Math.Max(MinWidth, _settings.Window.Width);
        Height = Math.Max(MinHeight, _settings.Window.Height);

        var alpha = (byte)MapOpacityToByte(Math.Clamp(_settings.BackgroundOpacity, 0.1, 1.0));
        _background.Background = new SolidColorBrush(Color.FromArgb(alpha, 28, 32, 36));
    }

    private static ResourceDictionary CreateTransparentChromeResources()
    {
        const string xaml = """
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <SolidColorBrush x:Key="TopMemoChromeBorder" Color="#55FFFFFF" />
    <SolidColorBrush x:Key="TopMemoTabBackground" Color="#331C2024" />
    <SolidColorBrush x:Key="TopMemoTabSelectedBackground" Color="#CC20242A" />
    <SolidColorBrush x:Key="TopMemoScrollTrack" Color="#22000000" />
    <SolidColorBrush x:Key="TopMemoScrollThumb" Color="#66FFFFFF" />
    <SolidColorBrush x:Key="TopMemoScrollThumbHover" Color="#99FFFFFF" />

    <Style TargetType="{x:Type TabControl}">
        <Setter Property="Background" Value="Transparent" />
        <Setter Property="BorderBrush" Value="{StaticResource TopMemoChromeBorder}" />
    </Style>

    <Style TargetType="{x:Type TabItem}">
        <Setter Property="Foreground" Value="White" />
        <Setter Property="Background" Value="{StaticResource TopMemoTabBackground}" />
        <Setter Property="BorderBrush" Value="{StaticResource TopMemoChromeBorder}" />
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="{x:Type TabItem}">
                    <Border x:Name="Border"
                            Background="{TemplateBinding Background}"
                            BorderBrush="{TemplateBinding BorderBrush}"
                            BorderThickness="1"
                            Padding="10,4"
                            Margin="0,0,2,0">
                        <ContentPresenter ContentSource="Header"
                                          HorizontalAlignment="Center"
                                          VerticalAlignment="Center"
                                          RecognizesAccessKey="True" />
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsSelected" Value="True">
                            <Setter TargetName="Border" Property="Background" Value="{StaticResource TopMemoTabSelectedBackground}" />
                            <Setter Property="Panel.ZIndex" Value="1" />
                        </Trigger>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="Border" Property="Background" Value="#552C3238" />
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <Style TargetType="{x:Type ScrollBar}">
        <Setter Property="Background" Value="{StaticResource TopMemoScrollTrack}" />
        <Setter Property="Foreground" Value="{StaticResource TopMemoScrollThumb}" />
        <Setter Property="Width" Value="14" />
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="{x:Type ScrollBar}">
                    <Grid Background="{TemplateBinding Background}">
                        <Track x:Name="PART_Track" IsDirectionReversed="True">
                            <Track.Thumb>
                                <Thumb Background="{TemplateBinding Foreground}">
                                    <Thumb.Template>
                                        <ControlTemplate TargetType="{x:Type Thumb}">
                                            <Border Background="{TemplateBinding Background}"
                                                    CornerRadius="4"
                                                    Margin="3" />
                                        </ControlTemplate>
                                    </Thumb.Template>
                                </Thumb>
                            </Track.Thumb>
                        </Track>
                    </Grid>
                    <ControlTemplate.Triggers>
                        <Trigger Property="Orientation" Value="Horizontal">
                            <Setter Property="Width" Value="Auto" />
                            <Setter Property="Height" Value="14" />
                            <Setter TargetName="PART_Track" Property="IsDirectionReversed" Value="False" />
                        </Trigger>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter Property="Foreground" Value="{StaticResource TopMemoScrollThumbHover}" />
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
</ResourceDictionary>
""";

        return (ResourceDictionary)XamlReader.Parse(xaml);
    }

    private void SetupTrayIcon()
    {
        if (_notifyIcon is not null)
        {
            return;
        }

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("表示", null, (_, _) => Dispatcher.Invoke(ShowMemo));
        menu.Items.Add("ファイル追加", null, (_, _) => Dispatcher.Invoke(AddFiles));
        menu.Items.Add("設定", null, (_, _) => Dispatcher.Invoke(OpenSettings));
        menu.Items.Add("全保存", null, (_, _) => Dispatcher.Invoke(SaveAllTabsWithStatus));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("終了", null, (_, _) => Dispatcher.Invoke(ExitApplication));

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "TopMemo2",
            ContextMenuStrip = menu,
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => Dispatcher.Invoke(ShowMemo);
    }

    private void LoadTabsFromSettings()
    {
        _tabControl.Items.Clear();
        _tabs.Clear();

        var loaded = 0;
        var failed = 0;
        foreach (var filePath in _settings.Files.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                AddTab(filePath);
                loaded++;
            }
            catch
            {
                failed++;
            }
        }

        if (_tabs.Count == 0)
        {
            _statusText.Text = "トレイメニューまたは上部ボタンから既存テキストファイルを追加します。";
        }
        else if (failed > 0)
        {
            _statusText.Text = $"{loaded} 件のファイルを開きました。{failed} 件は読み込めませんでした。";
        }
        else
        {
            _statusText.Text = $"{_tabs.Count} 件のファイルを開いています。";
        }
    }

    private void AddTab(string filePath)
    {
        var editor = new TextEditor
        {
            FontFamily = new FontFamily("Consolas"),
            FontSize = 14,
            ShowLineNumbers = true,
            Background = Brushes.Transparent,
            Foreground = Brushes.White,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Options =
            {
                ConvertTabsToSpaces = false,
                EnableRectangularSelection = true
            }
        };
        editor.TextArea.TextView.LineTransformers.Add(new HashHeadingColorizer());
        editor.Text = File.ReadAllText(filePath);

        var tab = new MemoTab(filePath, editor);
        editor.TextChanged += (_, _) => tab.IsDirty = true;

        var tabItem = new TabItem
        {
            Header = tab.Title,
            Content = editor,
            ToolTip = filePath
        };

        _tabs.Add(tab);
        _tabControl.Items.Add(tabItem);
        if (_tabControl.SelectedItem is null)
        {
            _tabControl.SelectedItem = tabItem;
        }
    }

    private void AddFiles()
    {
        ShowMemo();

        var dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            Multiselect = true,
            Filter = "テキストファイル|*.txt;*.md;*.log;*.csv;*.json;*.xml;*.yaml;*.yml;*.ini;*.cs;*.xaml|すべてのファイル|*.*"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var changed = false;
        foreach (var fileName in dialog.FileNames)
        {
            if (_settings.Files.Contains(fileName, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            _settings.Files.Add(fileName);
            AddTab(fileName);
            changed = true;
        }

        if (changed)
        {
            CaptureWindowSettings();
            _settingsStore.Save(_settings);
            _statusText.Text = "ファイル設定を保存しました。";
        }
    }

    private void OpenSettings()
    {
        ShowMemo();

        if (!SaveAllTabsWithStatus())
        {
            return;
        }

        var window = new FileSettingsWindow(_settings)
        {
            Owner = this
        };

        if (window.ShowDialog() != true || window.ResultSettings is null)
        {
            return;
        }

        _settings = window.ResultSettings;
        ApplySettings();
        LoadTabsFromSettings();
        CaptureWindowSettings();
        _settingsStore.Save(_settings);
        _statusText.Text = "設定を保存しました。";
    }

    private void OnMouseTimerTick(object? sender, EventArgs e)
    {
        if (!NativeMethods.GetCursorPos(out var point))
        {
            return;
        }

        var inHotCorner = IsInHotCorner(point);
        if (!IsVisible)
        {
            if (inHotCorner)
            {
                ShowMemo();
            }
            return;
        }

        var shouldStayVisible = inHotCorner || IsCursorInsideWindow(point);
        if (shouldStayVisible)
        {
            _hideDueAt = null;
            return;
        }

        _hideDueAt ??= DateTime.UtcNow.AddMilliseconds(Math.Max(0, _settings.HideDelayMilliseconds));
        if (DateTime.UtcNow >= _hideDueAt.Value)
        {
            HideWithSave();
        }
    }

    private bool IsInHotCorner(NativeMethods.POINT point)
    {
        var screen = Forms.Screen.PrimaryScreen?.Bounds ?? new System.Drawing.Rectangle(0, 0, 1920, 1080);
        var size = Math.Max(1, _settings.HotCorner.Size);
        return point.X >= screen.Left
            && point.Y >= screen.Top
            && point.X < screen.Left + size
            && point.Y < screen.Top + size;
    }

    private bool IsCursorInsideWindow(NativeMethods.POINT point)
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero || !NativeMethods.GetWindowRect(handle, out var rect))
        {
            return false;
        }

        return point.X >= rect.Left
            && point.X < rect.Right
            && point.Y >= rect.Top
            && point.Y < rect.Bottom;
    }

    private void ShowMemo()
    {
        _hideDueAt = null;
        if (!IsVisible)
        {
            Show();
        }

        Topmost = true;
        Activate();
    }

    private void HideWithSave()
    {
        if (!SaveAllTabsWithStatus())
        {
            _hideDueAt = null;
            return;
        }

        CaptureWindowSettings();
        _settingsStore.Save(_settings);
        _hideDueAt = null;
        Hide();
    }

    private bool SaveAllTabsWithStatus()
    {
        try
        {
            SaveAllTabs();
            _statusText.Text = "保存しました。";
            return true;
        }
        catch (Exception ex)
        {
            ShowMemo();
            _statusText.Text = $"保存に失敗しました: {ex.Message}";
            Forms.MessageBox.Show(ex.Message, "TopMemo2 保存エラー", Forms.MessageBoxButtons.OK, Forms.MessageBoxIcon.Error);
            return false;
        }
    }

    private void SaveAllTabs()
    {
        foreach (var tab in _tabs)
        {
            if (!tab.IsDirty)
            {
                continue;
            }

            File.WriteAllText(tab.FilePath, tab.Document.Text);
            tab.IsDirty = false;
        }
    }

    private void CaptureWindowSettings()
    {
        if (double.IsNaN(Left) || double.IsNaN(Top))
        {
            return;
        }

        _settings.Window.Left = Left;
        _settings.Window.Top = Top;
        _settings.Window.Width = Math.Max(MinWidth, Width);
        _settings.Window.Height = Math.Max(MinHeight, Height);
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isExiting)
        {
            return;
        }

        e.Cancel = true;
        HideWithSave();
    }

    private void ExitApplication()
    {
        if (!SaveAllTabsWithStatus())
        {
            return;
        }

        CaptureWindowSettings();
        _settingsStore.Save(_settings);
        _isExiting = true;
        _notifyIcon?.Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    private static int MapOpacityToByte(double opacity)
    {
        return (int)Math.Round(opacity * 255);
    }
}
