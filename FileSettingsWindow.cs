using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using TopMemo2.Models;
using Button = System.Windows.Controls.Button;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using ListBox = System.Windows.Controls.ListBox;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Orientation = System.Windows.Controls.Orientation;
using TextBox = System.Windows.Controls.TextBox;

namespace TopMemo2;

public sealed class FileSettingsWindow : Window
{
    private readonly ListBox _fileList = new();
    private readonly TextBox _hotCornerSize = new();
    private readonly TextBox _hideDelay = new();
    private readonly TextBox _backgroundOpacity = new();
    private readonly TextBox _windowWidth = new();
    private readonly TextBox _windowHeight = new();
    private readonly TextBox _fontFamily = new();
    private readonly TextBox _fontSize = new();
    private readonly List<string> _files;

    public AppSettings? ResultSettings { get; private set; }

    public FileSettingsWindow(AppSettings source)
    {
        _files = [.. source.Files];
        ResultSettings = Clone(source);
        BuildUi(source);
        RefreshFileList();
    }

    private void BuildUi(AppSettings source)
    {
        Title = "TopMemo2 設定";
        Width = 620;
        Height = 520;
        MinWidth = 520;
        MinHeight = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var root = new DockPanel
        {
            Margin = new Thickness(12)
        };
        Content = root;

        var bottom = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0)
        };
        DockPanel.SetDock(bottom, Dock.Bottom);
        root.Children.Add(bottom);

        var ok = new Button { Content = "OK", Width = 88, Height = 30, Margin = new Thickness(4, 0, 0, 0) };
        ok.Click += (_, _) => Accept();
        bottom.Children.Add(ok);

        var cancel = new Button { Content = "キャンセル", Width = 88, Height = 30, Margin = new Thickness(4, 0, 0, 0) };
        cancel.Click += (_, _) =>
        {
            DialogResult = false;
            Close();
        };
        bottom.Children.Add(cancel);

        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.Children.Add(layout);

        var filePanel = new Grid();
        filePanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        filePanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetRow(filePanel, 0);
        layout.Children.Add(filePanel);

        _fileList.MinHeight = 220;
        Grid.SetColumn(_fileList, 0);
        filePanel.Children.Add(_fileList);

        var fileButtons = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
        Grid.SetColumn(fileButtons, 1);
        filePanel.Children.Add(fileButtons);
        fileButtons.Children.Add(CreateSideButton("追加", (_, _) => AddFiles()));
        fileButtons.Children.Add(CreateSideButton("削除", (_, _) => RemoveSelected()));
        fileButtons.Children.Add(CreateSideButton("上へ", (_, _) => MoveSelected(-1)));
        fileButtons.Children.Add(CreateSideButton("下へ", (_, _) => MoveSelected(1)));

        var form = new Grid { Margin = new Thickness(0, 12, 0, 0) };
        form.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
        form.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (var i = 0; i < 7; i++)
        {
            form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }
        Grid.SetRow(form, 1);
        layout.Children.Add(form);

        AddFormRow(form, 0, "ホットコーナーサイズ", _hotCornerSize, source.HotCorner.Size.ToString());
        AddFormRow(form, 1, "非表示待機ミリ秒", _hideDelay, source.HideDelayMilliseconds.ToString());
        AddFormRow(form, 2, "背景不透明度", _backgroundOpacity, source.BackgroundOpacity.ToString("0.##"));
        AddFormRow(form, 3, "ウィンドウ幅", _windowWidth, source.Window.Width.ToString("0"));
        AddFormRow(form, 4, "ウィンドウ高さ", _windowHeight, source.Window.Height.ToString("0"));
        AddFormRow(form, 5, "フォント名", _fontFamily, GetFontFamily(source));
        AddFormRow(form, 6, "フォントサイズ", _fontSize, GetFontSize(source).ToString("0.##"));
    }

    private static Button CreateSideButton(string text, RoutedEventHandler handler)
    {
        var button = new Button
        {
            Content = text,
            Width = 88,
            Height = 30,
            Margin = new Thickness(0, 0, 0, 6)
        };
        button.Click += handler;
        return button;
    }

    private static void AddFormRow(Grid form, int row, string label, TextBox input, string value)
    {
        var text = new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 8)
        };
        Grid.SetRow(text, row);
        Grid.SetColumn(text, 0);
        form.Children.Add(text);

        input.Text = value;
        input.Margin = new Thickness(0, 0, 0, 8);
        Grid.SetRow(input, row);
        Grid.SetColumn(input, 1);
        form.Children.Add(input);
    }

    private void AddFiles()
    {
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

        foreach (var fileName in dialog.FileNames)
        {
            if (!_files.Contains(fileName, StringComparer.OrdinalIgnoreCase))
            {
                _files.Add(fileName);
            }
        }

        RefreshFileList();
    }

    private void RemoveSelected()
    {
        if (_fileList.SelectedItem is not string selected)
        {
            return;
        }

        var index = _files.FindIndex(path => string.Equals(path, selected, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            _files.RemoveAt(index);
            RefreshFileList(Math.Min(index, _files.Count - 1));
        }
    }

    private void MoveSelected(int direction)
    {
        if (_fileList.SelectedItem is not string selected)
        {
            return;
        }

        var index = _files.FindIndex(path => string.Equals(path, selected, StringComparison.OrdinalIgnoreCase));
        var next = index + direction;
        if (index < 0 || next < 0 || next >= _files.Count)
        {
            return;
        }

        (_files[index], _files[next]) = (_files[next], _files[index]);
        RefreshFileList(next);
    }

    private void RefreshFileList(int selectedIndex = -1)
    {
        _fileList.ItemsSource = null;
        _fileList.ItemsSource = _files;
        if (selectedIndex >= 0 && selectedIndex < _files.Count)
        {
            _fileList.SelectedIndex = selectedIndex;
        }
    }

    private void Accept()
    {
        if (ResultSettings is null)
        {
            return;
        }

        if (_files.Any(path => !File.Exists(path)))
        {
            MessageBox.Show(this, "存在しないファイルが含まれています。", "TopMemo2 設定", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryParseInt(_hotCornerSize.Text, 1, 200, out var hotCornerSize, "ホットコーナーサイズ"))
        {
            return;
        }

        if (!TryParseInt(_hideDelay.Text, 0, 60000, out var hideDelay, "非表示待機ミリ秒"))
        {
            return;
        }

        if (!TryParseDouble(_backgroundOpacity.Text, 0.1, 1.0, out var opacity, "背景不透明度"))
        {
            return;
        }

        if (!TryParseDouble(_windowWidth.Text, 320, 4000, out var width, "ウィンドウ幅"))
        {
            return;
        }

        if (!TryParseDouble(_windowHeight.Text, 240, 3000, out var height, "ウィンドウ高さ"))
        {
            return;
        }

        var fontFamily = _fontFamily.Text.Trim();
        if (string.IsNullOrWhiteSpace(fontFamily))
        {
            MessageBox.Show(this, "フォント名は空にしません。", "TopMemo2 設定", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryParseDouble(_fontSize.Text, 6, 72, out var fontSize, "フォントサイズ"))
        {
            return;
        }

        ResultSettings.Files = [.. _files];
        ResultSettings.HotCorner.Size = hotCornerSize;
        ResultSettings.HideDelayMilliseconds = hideDelay;
        ResultSettings.BackgroundOpacity = opacity;
        ResultSettings.Window.Width = width;
        ResultSettings.Window.Height = height;
        ResultSettings.Font.Family = fontFamily;
        ResultSettings.Font.Size = fontSize;

        DialogResult = true;
        Close();
    }

    private bool TryParseInt(string text, int min, int max, out int value, string label)
    {
        if (!int.TryParse(text, out value) || value < min || value > max)
        {
            MessageBox.Show(this, $"{label} は {min} から {max} の整数にします。", "TopMemo2 設定", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        return true;
    }

    private bool TryParseDouble(string text, double min, double max, out double value, string label)
    {
        if (!double.TryParse(text, out value) || value < min || value > max)
        {
            MessageBox.Show(this, $"{label} は {min} から {max} の数値にします。", "TopMemo2 設定", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        return true;
    }

    private static AppSettings Clone(AppSettings source)
    {
        return new AppSettings
        {
            Files = [.. source.Files],
            HideDelayMilliseconds = source.HideDelayMilliseconds,
            BackgroundOpacity = source.BackgroundOpacity,
            Font = new FontSettings
            {
                Family = GetFontFamily(source),
                Size = GetFontSize(source)
            },
            HotCorner = new HotCornerSettings
            {
                Size = source.HotCorner.Size
            },
            Window = new WindowSettings
            {
                Left = source.Window.Left,
                Top = source.Window.Top,
                Width = source.Window.Width,
                Height = source.Window.Height
            }
        };
    }

    private static string GetFontFamily(AppSettings settings)
    {
        return string.IsNullOrWhiteSpace(settings.Font?.Family)
            ? new FontSettings().Family
            : settings.Font.Family;
    }

    private static double GetFontSize(AppSettings settings)
    {
        return settings.Font?.Size is >= 6 and <= 72
            ? settings.Font.Size
            : new FontSettings().Size;
    }
}
