using System;
using System.Windows.Forms;

namespace ChatClient.Controls
{
    /// <summary>
    /// Custom ListView với Double Buffering để tránh flicker khi OwnerDraw
    /// </summary>
    public class DoubleBufferedListView : ListView
    {
        public DoubleBufferedListView()
        {
            // Enable double buffering để tránh flicker
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | 
                          ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.EnableNotifyMessage, true);
            
            this.DoubleBuffered = true;
        }
        
        protected override void OnNotifyMessage(Message m)
        {
            // Filter out WM_ERASEBKGND để tránh flicker
            if (m.Msg != 0x14) // WM_ERASEBKGND
            {
                base.OnNotifyMessage(m);
            }
        }
        
        protected override void WndProc(ref Message m)
        {
            // Ignore WM_ERASEBKGND để tránh flicker background
            if (m.Msg == 0x14) // WM_ERASEBKGND
            {
                m.Result = IntPtr.Zero;
                return;
            }
            
            base.WndProc(ref m);
        }
    }
}
