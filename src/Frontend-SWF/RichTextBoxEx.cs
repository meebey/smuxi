using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace Smuxi.Frontend.Swf
{
    class RichTextBoxEx : RichTextBox
    {
        private const int WM_VSCROLL =  0x115;
        private const int WM_HSCROLL =  0x114;

        private const int SB_LINEUP =   0;
        private const int SB_LINEDOWN = 1;
        private const int SB_PAGEUP =   2;
        private const int SB_PAGEDOWN = 3;
        private const int SB_TOP =      6;
        private const int SB_BOTTOM =   7;

        public bool CaretEndPosition {
            get {
                return SelectionStart == TextLength;
            }
        }

        public RichTextBoxEx() : base()
        {
        }

        private IntPtr SendMessage(int msg, IntPtr wParam, IntPtr lParam)
        {
            Message m = new Message();
            m.HWnd = Handle;
            m.Msg = msg;
            m.WParam = wParam;
            m.LParam = lParam;
            WndProc(ref m);
            return m.Result;
        }
        
        public void ScrollToEnd()
        {
            SendMessage(WM_VSCROLL, (IntPtr)SB_BOTTOM, IntPtr.Zero);
        }

        public void SetCaretEndPosition()
        {
            SelectionStart = TextLength;
        }
    }
}
