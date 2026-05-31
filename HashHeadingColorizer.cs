using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace TopMemo2;

public sealed class HashHeadingColorizer : DocumentColorizingTransformer
{
    protected override void ColorizeLine(DocumentLine line)
    {
        var text = CurrentContext.Document.GetText(line);
        if (!text.StartsWith('#'))
        {
            return;
        }

        ChangeLinePart(line.Offset, line.EndOffset, element =>
        {
            var typeface = element.TextRunProperties.Typeface;
            element.TextRunProperties.SetTypeface(new Typeface(
                typeface.FontFamily,
                typeface.Style,
                FontWeights.Bold,
                typeface.Stretch));
        });
    }
}
