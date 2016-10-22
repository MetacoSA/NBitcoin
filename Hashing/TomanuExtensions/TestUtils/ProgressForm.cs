using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace TomanuExtensions.TestUtils
{
    public partial class ProgressForm : Form
    {
        public AutoResetEvent CreatedEvent = new AutoResetEvent(false);

        public ProgressForm()
        {
            InitializeComponent();
        }

        public void AddLine(string a_line)
        {
            if (richTextBox.Lines.Length > 0)
                richTextBox.AppendText(System.Environment.NewLine);
            richTextBox.AppendText(a_line);

            richTextBox.SelectionStart = richTextBox.TextLength;
            richTextBox.ScrollToCaret();
        }

        public void UpdateLastLine(string a_line)
        {
            if (richTextBox.Lines.Length > 0)
                DeleteLine(richTextBox.Lines.Count() - 1);

            AddLine(a_line);
        }

        private void DeleteLine(int a_line)
        {
            int start_index = richTextBox.GetFirstCharIndexFromLine(a_line);
            int count = richTextBox.Lines[a_line].Length;

            if (a_line < richTextBox.Lines.Length - 1)
            {
                count += richTextBox.GetFirstCharIndexFromLine(a_line + 1) -
                    ((start_index + count - 1) + 1);
            }

            richTextBox.Text = richTextBox.Text.Remove(start_index, count);
        }

        private void ProgressForm_Shown(object sender, EventArgs e)
        {
            CreatedEvent.Set();
        }
    }
}
