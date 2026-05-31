using System.IO;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;

namespace TopMemo2;

public sealed class MemoTab
{
    public MemoTab(string filePath, TextEditor editor)
    {
        FilePath = filePath;
        Editor = editor;
        Document = editor.Document;
    }

    public string FilePath { get; }
    public TextEditor Editor { get; }
    public TextDocument Document { get; }
    public bool IsDirty { get; set; }
    public string Title => Path.GetFileName(FilePath);
}
