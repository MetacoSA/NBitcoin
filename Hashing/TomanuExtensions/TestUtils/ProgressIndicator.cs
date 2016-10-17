using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace TomanuExtensions.TestUtils
{
    public class ProgressIndicator
    {
        private Thread m_thread;
        private ProgressForm m_form = new ProgressForm();

        public ProgressIndicator(string a_title)
        {
            m_form.Text = a_title;

            m_thread = new Thread(() =>
            {
                Application.EnableVisualStyles();
                Application.Run(m_form);
                m_thread = null;
            });

            m_thread.SetApartmentState(ApartmentState.MTA);
            m_thread.Start();
            m_form.CreatedEvent.WaitOne();
            m_form.CreatedEvent.Close();
        }

        public bool IsDisposed
        {
            get
            {
                return m_form.IsDisposed;
            }
        }

        private T Invoke<T>(Func<T> a_delegate)
        {
            if (m_form.InvokeRequired)
            {
                try
                {
                    return (T)m_form.Invoke(a_delegate);
                }
                catch (ObjectDisposedException)
                {
                    return default(T);
                }
            }
            else if (m_form.IsHandleCreated)
                return a_delegate();
            else
                return default(T);
        }

        private void Invoke(Action a_delegate)
        {
            if (m_form.InvokeRequired)
                m_form.BeginInvoke(a_delegate);
            else if (m_form.IsHandleCreated)
                a_delegate();
        }

        public void AddLine(string a_line)
        {
            Invoke(() => { m_form.AddLine(a_line); });
        }

        public void UpdateLastLine(string a_line)
        {
            Invoke(() => { m_form.UpdateLastLine(a_line); });
        }
    }
}
