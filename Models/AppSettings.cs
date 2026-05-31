namespace TopMemo2.Models;

public sealed class AppSettings
{
    public List<string> Files { get; set; } = [];
    public WindowSettings Window { get; set; } = new();
    public HotCornerSettings HotCorner { get; set; } = new();
    public int HideDelayMilliseconds { get; set; } = 2000;
    public double BackgroundOpacity { get; set; } = 0.85;
}

public sealed class WindowSettings
{
    public double Left { get; set; } = 80;
    public double Top { get; set; } = 80;
    public double Width { get; set; } = 720;
    public double Height { get; set; } = 520;
}

public sealed class HotCornerSettings
{
    public int Size { get; set; } = 3;
}
