using System.Diagnostics;
using System.Windows.Forms;

namespace TomanuExtensions
{
    [DebuggerStepThrough]
    public static class RichTextBoxExtensions
    {
        public static void ScrollToEnd(this RichTextBox a_edit)
        {
            a_edit.SelectionStart = a_edit.Text.Length;
            a_edit.ScrollToCaret();
        }
    }
}