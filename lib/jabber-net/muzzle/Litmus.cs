/* --------------------------------------------------------------------------
 * Copyrights
 *
 * Portions created by or assigned to Cursive Systems, Inc. are
 * Copyright (c) 2002-2007 Cursive Systems, Inc.  All Rights Reserved.  Contact
 * information for Cursive Systems, Inc. is available at
 * http://www.cursive.net/.
 *
 * License
 *
 * Jabber-Net can be used under either JOSL or the GPL.
 * See LICENSE.txt for details.
 * --------------------------------------------------------------------------*/
using System;

using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace muzzle
{
    /// <summary>
    /// How should colors be picked?
    /// </summary>
    public enum LitmusColorScheme
    {
        /// <summary>
        /// Just shades of blue
        /// </summary>
        Blue,
        /// <summary>
        /// More colors for non-ASCII
        /// </summary>
        Multicolor
    }
    /// <summary>
    /// Litmus is like StripChart, but shows a graphical representation of protocol going by.
    /// This was inspired by DW &amp; Craig&apos;s suggestion that the next generation protocol should
    /// just be shades of blue.
    ///
    /// Good gracious.  Did I really take the time to write this?
    /// </summary>
    public class Litmus : System.Windows.Forms.UserControl
    {
        private int               m_hist  = -1;
        private int               m_max   = 0;
        private bool              m_pause = false;
        private Queue             m_list  = new Queue(100);
        private LitmusColorScheme m_scheme = LitmusColorScheme.Blue;
        private System.Windows.Forms.PictureBox pictureBox1;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        /// <summary>
        /// Create a new Litmus object
        /// </summary>
        public Litmus()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();
            // TODO: Add any initialization after the InitForm call
        }
        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing )
        {
            if( disposing )
            {
                if(components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose( disposing );
        }
        /// <summary>
        /// Color scheme to use.
        /// </summary>
        [Description("Color scheme to use")]
        [DefaultValue(LitmusColorScheme.Blue)]
        [Category("Chart")]
        public LitmusColorScheme ColorScheme
        {
            get
            {
                return m_scheme;
            }
            set
            {
                m_scheme = value;
            }
        }
        /// <summary>
        /// Number of points to show.  -1 means all
        /// </summary>
        [Description("Number of points to show.  -1 means all")]
        [DefaultValue(-1)]
        [Category("Chart")]
        public int History
        {
            get
            {
                return m_hist;
            }
            set
            {
                m_hist = value;
            }
        }
        /// <summary>
        /// Don't update the display for now.  Useful for bulk loads.
        /// </summary>
        public bool Paused
        {
            get
            {
                return m_pause;
            }
            set
            {
                m_pause = value;
                ReDraw();
            }
        }

        /// <summary>
        /// Clear all data in the window
        /// </summary>
        public void Clear()
        {
            m_list.Clear();
            ReDraw();
        }

        /// <summary>
        /// Add a string to the window.  Each byte will become roughly a
        /// pixel with color based on the byte's value.
        /// </summary>
        /// <param name="text"></param>
        public void AddText(string text)
        {
            if (m_hist != -1)
                while (m_list.Count > m_hist)
                    m_list.Dequeue();
            byte[] buf = System.Text.Encoding.UTF8.GetBytes(text);
            m_list.Enqueue(buf);
            if (buf.Length > m_max)
                m_max = buf.Length;
            ReDraw();
        }

        /// <summary>
        /// Add bytes to the window.  Each byte will become roughly a
        /// pixel with color based on the byte's value.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        public void AddBytes(byte[] buf, int offset, int length)
        {
            if (m_hist != -1)
                while (m_list.Count > m_hist)
                    m_list.Dequeue();
            byte[] copy = new byte[length];
            Array.Copy(buf, offset, copy, 0, length);
            m_list.Enqueue(copy);
            if (length > m_max)
                m_max = length;
            ReDraw();
        }
        private void ReDraw()
        {
            if (m_pause)
                return;
            Bitmap bm = new Bitmap(this.Width, this.Height);
            Graphics g = Graphics.FromImage(bm);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(this.BackColor);
            SolidBrush brush = new SolidBrush(this.ForeColor);
            float h = this.Height;
            float w = this.Width;
            float stripw = w / ((float)m_list.Count - 1F);
            float striph = h / ((float)m_max  - 1F);
            int sc = 0;
            int cc = 0;
            switch (m_scheme)
            {
                case LitmusColorScheme.Blue:
                    foreach (byte[] buf in m_list)
                    {
                        cc = 0;
                        foreach (byte b in buf)
                        {
                            /*
                            */
                            brush.Color = Color.FromArgb(0, 0, 255 - b);
                            g.FillRectangle(brush, sc * stripw, cc * striph, stripw, striph);
                            cc++;
                        }
                        sc++;
                    }
                    break;
                case LitmusColorScheme.Multicolor:
                    foreach (byte[] buf in m_list)
                    {
                        cc = 0;
                        foreach (byte b in buf)
                        {
                            if (b == 0)
                                brush.Color = Color.White;
                            else if (b < 65)
                                brush.Color = Color.FromArgb(b * 4 - 1, 0, 0);
                            else if (b < 128)
                                brush.Color = Color.FromArgb(0, 0, b * 2);
                            else
                                brush.Color = Color.FromArgb(0, (b - 128) * 2, 0);
                            g.FillRectangle(brush, sc * stripw, cc * striph, stripw, striph);
                            cc++;
                        }
                        sc++;
                    }
                    break;
            }
            pictureBox1.Image = bm;
        }
        #region Component Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.SuspendLayout();
            //
            // pictureBox1
            //
            this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(150, 150);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            //
            // Litmus
            //
            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                                                                          this.pictureBox1});
            this.Name = "Litmus";
            this.Resize += new System.EventHandler(this.Litmus_Resize);
            this.Load += new System.EventHandler(this.Litmus_Load);
            this.ResumeLayout(false);

        }
        #endregion
        private void Litmus_Load(object sender, System.EventArgs e)
        {
            ReDraw();
        }
        private void Litmus_Resize(object sender, System.EventArgs e)
        {
            ReDraw();
        }
    }
}
