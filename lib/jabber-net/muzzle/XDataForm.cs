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

using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Xml;

using jabber.protocol;
using jabber.protocol.x;
using jabber.protocol.client;
using Msg = jabber.protocol.client.Message;

namespace muzzle
{
    /// <summary>
    /// Summary description for XData.
    /// </summary>
    public class XDataForm : System.Windows.Forms.Form
    {
        private static Regex WS = new Regex("\\s+", RegexOptions.Compiled);

        private Packet      m_parent   = null;
        private FormField[] m_fields   = null;
        private XDataType   m_type;

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Label lblInstructions;
        private System.Windows.Forms.Panel pnlFields;
        private System.Windows.Forms.ErrorProvider error;
        private System.Windows.Forms.ToolTip tip;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel3;
        private System.ComponentModel.IContainer components;

        /// <summary>
        /// Create an x:data form with no contents.
        /// </summary>
        public XDataForm()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();
        }

        /// <summary>
        /// Create an x:data form from the given message stanza.
        /// </summary>
        /// <param name="parent">Original stanza</param>
        public XDataForm(jabber.protocol.client.Message parent) : this(parent["x", URI.XDATA] as jabber.protocol.x.Data)
        {
            m_parent = (Packet) parent.CloneNode(true);
            m_parent.RemoveChild(m_parent["x", URI.XDATA]);
        }

        /// <summary>
        /// Create an x:data form from the given iq stanza.
        /// </summary>
        /// <param name="parent">Original stanza</param>
        public XDataForm(jabber.protocol.client.IQ parent) : this(parent.Query["x", URI.XDATA] as jabber.protocol.x.Data)
        {
            m_parent = (Packet) parent.CloneNode(true);
            XmlElement q = m_parent.GetFirstChildElement();
            q.RemoveChild(q["x", URI.XDATA]);
        }

        /// <summary>
        /// Create an x:data form from the given XML form description
        /// </summary>
        /// <param name="x">x:data form to render</param>
        public XDataForm(jabber.protocol.x.Data x) : this()
        {
            if (x == null)
                throw new ArgumentException("x:data form must not be null", "x");

            m_type = x.Type;

            this.SuspendLayout();
            if (x.Title != null)
                this.Text = x.Title;
            if (m_type == XDataType.cancel)
            {
                lblInstructions.Text = "Form canceled.";  // TODO: Localize!
                lblInstructions.Resize += new EventHandler(lblInstructions_Resize);
                lblInstructions_Resize(lblInstructions, null);
            }
            else if (x.Instructions == null)
                lblInstructions.Visible = false;
            else
            {
                lblInstructions.Text = DeWhitespace(x.Instructions);
                lblInstructions.Resize += new EventHandler(lblInstructions_Resize);
                lblInstructions_Resize(lblInstructions, null);
            }

            pnlFields.SuspendLayout();
            Field[] fields = x.GetFields();
            m_fields = new FormField[fields.Length];
            for (int i=fields.Length-1; i>=0; i--)
            {
                m_fields[i] = new FormField(fields[i], this, i);
            }

            panel1.TabIndex = fields.Length;

            if (m_type != XDataType.form)
            {
                btnOK.Location = btnCancel.Location;
                btnCancel.Visible = false;
            }

            pnlFields.ResumeLayout(true);
            this.ResumeLayout(true);

            for (int i=0; i<fields.Length; i++)
            {
                if ((fields[i].Type != FieldType.hidden) &&
                    (fields[i].Type != FieldType.Fixed))
                    m_fields[i].Focus();
            }
        }

        private string DeWhitespace(string input)
        {
            return WS.Replace(input, " ");
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

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.lblInstructions = new System.Windows.Forms.Label();
            this.pnlFields = new System.Windows.Forms.Panel();
            this.error = new System.Windows.Forms.ErrorProvider();
            this.tip = new System.Windows.Forms.ToolTip(this.components);
            this.panel3 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            //
            // panel1
            //
            this.panel1.CausesValidation = false;
            this.panel1.Controls.Add(this.panel2);
            this.panel1.Controls.Add(this.btnCancel);
            this.panel1.Controls.Add(this.btnOK);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 232);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(292, 34);
            this.panel1.TabIndex = 2;
            //
            // panel2
            //
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(292, 4);
            this.panel2.TabIndex = 2;
            //
            // btnCancel
            //
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.CausesValidation = false;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(212, 7);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            //
            // btnOK
            //
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Location = new System.Drawing.Point(132, 7);
            this.btnOK.Name = "btnOK";
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "OK";
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            //
            // lblInstructions
            //
            this.lblInstructions.BackColor = System.Drawing.SystemColors.Control;
            this.lblInstructions.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblInstructions.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.lblInstructions.Location = new System.Drawing.Point(0, 0);
            this.lblInstructions.Name = "lblInstructions";
            this.lblInstructions.Size = new System.Drawing.Size(292, 16);
            this.lblInstructions.TabIndex = 1;
            //
            // pnlFields
            //
            this.pnlFields.AutoScroll = true;
            this.pnlFields.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlFields.DockPadding.Bottom = 3;
            this.pnlFields.DockPadding.Left = 6;
            this.pnlFields.DockPadding.Right = 6;
            this.pnlFields.DockPadding.Top = 3;
            this.pnlFields.Location = new System.Drawing.Point(0, 20);
            this.pnlFields.Name = "pnlFields";
            this.pnlFields.Size = new System.Drawing.Size(292, 212);
            this.pnlFields.TabIndex = 0;
            //
            // error
            //
            this.error.ContainerControl = this;
            //
            // panel3
            //
            this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel3.Location = new System.Drawing.Point(0, 16);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(292, 4);
            this.panel3.TabIndex = 3;
            //
            // XDataForm
            //
            this.AcceptButton = this.btnOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(292, 266);
            this.ControlBox = false;
            this.Controls.Add(this.pnlFields);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.lblInstructions);
            this.HelpButton = true;
            this.Name = "XDataForm";
            this.Text = "XData Form";
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

        private void btnOK_Click(object sender, System.EventArgs e)
        {
            for (int i=0; i<m_fields.Length; i++)
            {
                if (!m_fields[i].Validate())
                {
                    m_fields[i].Focus();
                    return;
                }
            }
            this.DialogResult = DialogResult.OK;
        }

        private void lblInstructions_Resize(object sender, EventArgs e)
        {
            Graphics graphics = lblInstructions.CreateGraphics();
            SizeF s = lblInstructions.Size;
            s.Height = 0;
            SizeF textSize = graphics.MeasureString(lblInstructions.Text, lblInstructions.Font, s);
            lblInstructions.Height = (int) (textSize.Height) + 2;
        }

        /// <summary>
        /// Get a response to the original stanza that caused this form to be popped up.
        /// </summary>
        /// <returns>A stanza ready to be sent back to the originator.</returns>
        public Packet GetResponse()
        {
            if (m_parent == null)
                throw new ArgumentException("parent was null", "parent");
            if (m_type != XDataType.form)
                throw new InvalidOperationException("Can only generate a submit response for x:data of type 'form'");

            m_parent.Swap();

            Data x = new Data(m_parent.OwnerDocument);
            if (m_parent is Msg)
            {
                m_parent.AppendChild(x);
            }
            else if (m_parent is IQ)
            {
                m_parent.GetFirstChildElement().AppendChild(x);
                m_parent.SetAttribute("type", "result");
            }
            else
            {
                throw new ArgumentException("unknown parent type", "parent");
            }

            if (this.DialogResult == DialogResult.Cancel)
            {
                x.Type = XDataType.cancel;
                return m_parent;
            }

            x.Type = XDataType.submit;
            foreach (FormField f in m_fields)
            {
                f.AppendField(x);
            }

            return m_parent;
        }

        private class FormField
        {
            private FieldType m_type;
            private string m_var;
            private string[] m_val;
            private XDataForm m_form;
            private bool m_required = false;
            private Control m_control = null;
            private Label m_label = null;
            private Field m_field;

            public FormField(Field f, XDataForm form, int tabIndex)
            {
                m_field = f;
                m_type = f.Type;
                m_var = f.Var;
                m_val = f.Vals;
                m_required = f.IsRequired;
                m_form = form;

                Panel p = null;
                if (m_type != FieldType.hidden)
                {
                    p = new Panel();
                    p.Parent = m_form.pnlFields;
                    p.TabIndex = tabIndex;
                }

                switch (m_type)
                {
                    case FieldType.hidden:
                        break;
                    case FieldType.boolean:
                        CheckBox cb = new CheckBox();
                        cb.Checked = f.BoolVal;
                        cb.Text = null;
                        m_control = cb;
                        break;
                    case FieldType.text_multi:
                        TextBox mtxt = new TextBox();
                        mtxt.Multiline = true;
                        mtxt.ScrollBars = ScrollBars.Vertical;
                        mtxt.Lines = m_val;
                        mtxt.Height = m_form.btnOK.Height * 3;
                        m_control = mtxt;
                        break;
                    case FieldType.text_private:
                        TextBox ptxt = new TextBox();
                        ptxt.Lines = m_val;
                        ptxt.PasswordChar = '*';
                        m_control = ptxt;
                        break;
                    case FieldType.list_single:
                        ComboBox box = new ComboBox();
                        box.DropDownStyle = ComboBoxStyle.DropDownList;
                        box.BeginUpdate();
                        string v = null;
                        if (m_val.Length > 0)
                            v = m_val[0];
                        foreach (Option o in f.GetOptions())
                        {
                            int i = box.Items.Add(o);

                            if (o.Val == v)
                            {
                                box.SelectedIndex = i;
                            }
                        }
                        box.EndUpdate();
                        m_control = box;
                        break;

                    case FieldType.list_multi:
                        //ListBox lb = new ListBox();
                        CheckedListBox lb = new CheckedListBox();
                        //lb.SelectionMode = SelectionMode.MultiExtended;
                        lb.VisibleChanged += new EventHandler(lb_VisibleChanged);
                        m_control = lb;
                        break;

                    case FieldType.jid_single:
                        TextBox jtxt = new TextBox();
                        jtxt.Lines = m_val;
                        jtxt.Validating += new CancelEventHandler(jid_Validating);
                        jtxt.Validated += new EventHandler(jid_Validated);
                        m_control = jtxt;
                        m_form.error.SetIconAlignment(m_control, ErrorIconAlignment.MiddleLeft);
                        break;

                    case FieldType.jid_multi:
                        JidMulti multi = new JidMulti();
                        multi.AddRange(m_val);
                        m_control = multi;
                        break;

                    case FieldType.Fixed:
                        // All of this so that we can detect URLs.
                        // We can't just make it disabled, because then the URL clicked
                        // event handler doesn't fire, and there's no way to set the
                        // text foreground color.
                        // It would be cool to make copy work, but it doesn't work for
                        // labels, either.
                        RichTextBox rich = new RichTextBox();
                        rich.DetectUrls = true;
                        rich.Text = string.Join("\r\n", f.Vals);
                        rich.ScrollBars = RichTextBoxScrollBars.None;
                        rich.Resize += new EventHandler(lbl_Resize);
                        rich.BorderStyle = BorderStyle.None;
                        rich.LinkClicked += new LinkClickedEventHandler(rich_LinkClicked);
                        rich.BackColor = System.Drawing.SystemColors.Control;
                        rich.KeyPress += new KeyPressEventHandler(rich_KeyPress);
                        rich.GotFocus += new EventHandler(rich_GotFocus);
                        rich.AcceptsTab = false;
                        rich.AutoSize = false;
                        m_control = rich;
                        break;
                    default:
                        TextBox txt = new TextBox();
                        txt.Lines = m_val;
                        m_control = txt;
                        break;
                }

                if (m_type != FieldType.hidden)
                {
                    m_control.Parent = p;

                    if (f.Desc != null)
                        form.tip.SetToolTip(m_control, f.Desc);

                    String lblText = "";

                    if (f.Label != "")
                        lblText = f.Label + ":";
                    else if (f.Var != "")
                        lblText = f.Var + ":";

                    if (lblText != "")
                    {
                        m_label = new Label();
                        m_label.Parent = p;
                        m_label.Text = lblText;

                        if (m_required)
                        {
                            m_label.Text = "* " + m_label.Text;
                            m_form.error.SetIconAlignment(m_control, ErrorIconAlignment.MiddleLeft);

                            m_control.Validating += new CancelEventHandler(m_control_Validating);
                            m_control.Validated += new EventHandler(m_control_Validated);
                        }
                        Graphics graphics = m_label.CreateGraphics();
                        SizeF s = m_label.Size;
                        s.Height = 0;
                        int chars;
                        int lines;
                        SizeF textSize = graphics.MeasureString(m_label.Text, m_label.Font, s, StringFormat.GenericDefault, out chars, out lines);
                        m_label.Height = (int) (textSize.Height);

                        if (lines > 1)
                            m_label.TextAlign = ContentAlignment.MiddleLeft;
                        else
                            m_label.TextAlign = ContentAlignment.TopLeft;

                        m_label.Top = 0;
                        p.Controls.Add(m_label);
                        m_control.Location = new Point(m_label.Width + 3, 0);
                        m_control.Width = p.Width - m_label.Width - 6;
                        p.Height = Math.Max(m_label.Height, m_control.Height) + 4;
                    }
                    else
                    {
                        m_control.Location = new Point(0, 0);
                        m_control.Width = p.Width - 6;
                        p.Height = m_control.Height + 4;
                    }
                    m_control.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    p.Controls.Add(m_control);
                    p.Dock = DockStyle.Top;
                    m_form.pnlFields.Controls.Add(p);

                    if (m_form.m_type != XDataType.form)
                        m_control.Enabled = false;
                }
            }

            public void Focus()
            {
                if (m_control != null)
                    m_control.Focus();
            }

            public string[] Value
            {
                get
                {
                    if (m_control == null)
                        return m_val;
                    if (m_control is TextBox)
                        return ((TextBox)m_control).Lines;
                    if (m_control is CheckBox)
                        return new string[] { ((CheckBox)m_control).Checked ? "1" : "0" };
                    if (m_control is ComboBox)
                    {
                        Option o = (Option)((ComboBox) m_control).SelectedItem;
                        if (o == null) return new string[] {};
                        return new string[] { o.Val };
                    }

                    return null;
                }
            }

            public bool Validate()
            {
                if (m_control == null)
                    return true;
                if (!m_required)
                    return true;

                if ((Value == null) || (Value.Length == 0))
                {
                    m_form.error.SetError(m_control, "Required");
                    return false;
                }

                return true;
            }

            private void m_control_Validating(object sender, CancelEventArgs e)
            {
                if (!Validate())
                    e.Cancel = true;
            }

            private void m_control_Validated(object sender, EventArgs e)
            {
                m_form.error.SetError(m_control, "");
            }

            private void jid_Validated(object sender, EventArgs e)
            {
                m_form.error.SetError(m_control, "");
            }

            private void lbl_Resize(object sender, EventArgs e)
            {
                RichTextBox lbl = (RichTextBox) sender;

                Graphics graphics = lbl.CreateGraphics();
                SizeF s = lbl.Size;
                s.Height = 0;
                SizeF textSize = graphics.MeasureString(lbl.Text, lbl.Font, s);
                lbl.Height = (int) (textSize.Height) + 4;
                if (e != null)
                {
                    lbl.Parent.Height = Math.Max(lbl.Height, m_control.Height) + 4;
                }
            }

            private void lb_VisibleChanged(object sender, EventArgs e)
            {
                // HACK: Oh.  My.  God.
                // This was found through trial and error, and I'm NOT happy with it.
                // The deal is that there is a bug in the MS implementation of ListBox, such
                // that if you call SetSelected before the underlying window has been created,
                // the SetSelected call gets ignored.

                // So, what we do here is wait for VisibleChanged events...  this is the only event
                // I could find that always fires after the Handle is set.  But, it also fires before
                // the handle is set, and several times so quickly in succession that the remove
                // event handler code below can happen while there is an event still in the queue.
                // Apparently that message that is already in the queue fires this callback again,
                // even though it's been removed.
                CheckedListBox lb = (CheckedListBox) sender;
                if (lb.Handle == IntPtr.Zero)
                    return;

                if (lb.Items.Count > 0)
                    return;

                lb.VisibleChanged -= new EventHandler(lb_VisibleChanged);

                lb.BeginUpdate();
                foreach (Option o in m_field.GetOptions())
                {
                    int i = lb.Items.Add(o);
                    if (m_field.IsValSet(o.Val))
                        //lb.SetSelected(i, true);
                        lb.SetItemChecked(i, true);
                }
                lb.EndUpdate();

            }

            private void jid_Validating(object sender, CancelEventArgs e)
            {
                TextBox jtxt = (TextBox) sender;
                if (jtxt.Text == "")
                    return;

                try
                {
                    jabber.JID j = new jabber.JID(jtxt.Text);
                }
                catch
                {
                    e.Cancel = true;
                    m_form.error.SetError(jtxt, "Invalid JID");
                }
            }

            public void AppendField(Data x)
            {
                String[] vals = this.Value;
                if ((vals != null) && (vals.Length > 0))
                {
                    Field f = x.AddField(m_var, m_type, null, null, null);
                    foreach (String v in vals)
                    {
                        f.AddValue(v);
                    }
                }
            }

            private void rich_LinkClicked(object sender, LinkClickedEventArgs e)
            {
                System.Diagnostics.Process.Start(e.LinkText);
            }

            private void rich_KeyPress(object sender, KeyPressEventArgs e)
            {
                e.Handled = true;
            }

            private void rich_GotFocus(object sender, EventArgs e)
            {
                // gyrate, trying to prevent focus from ever landing on the
                // richtext box.
                RichTextBox rich = (RichTextBox) sender;
                Control nxt = m_form.GetNextControl(rich, true);
                while ((nxt != null) && (nxt != rich) && ((!nxt.CanFocus) || (nxt is Panel)))
                    nxt = m_form.GetNextControl(nxt, true);
                if ((nxt != null) && (nxt != rich))
                    nxt.Focus();
            }
        }
    }
}
