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
using System.ComponentModel.Design;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;

using bedrock.util;
using jabber.connection;

namespace muzzle
{
    /// <summary>
    /// Base class for forms that configure XmppStream subclasses.
    /// </summary>
    [SVN(@"$Id: OptionForm.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class OptionForm : Form
    {
        private Button btnCancel;
        private Button btnOK;
        private Panel panel1;

        private XmppStream m_xmpp;
        private Hashtable m_extra = new Hashtable();

        /// <summary>
        /// ToolTips.
        /// </summary>
        protected ToolTip tip;
        /// <summary>
        /// Error notifications.
        /// </summary>
        protected ErrorProvider error;

        /// <summary>
        /// Required designer variable.
        /// </summary>
        protected System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Create new form
        /// </summary>
        protected OptionForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Create new form.
        /// </summary>
        /// <param name="xmpp"></param>
        protected OptionForm(XmppStream xmpp)
            : this()
        {
            m_xmpp = xmpp;
        }


        /// <summary>
        /// The client connection to manage
        /// </summary>
        public XmppStream Xmpp
        {
            get
            {
                // If we are running in the designer, let's try to auto-hook a JabberClient
                if ((m_xmpp == null) && DesignMode)
                {
                    IDesignerHost host = (IDesignerHost)base.GetService(typeof(IDesignerHost));
                    if (host != null)
                    {
                        Component root = host.RootComponent as Component;
                        if (root != null)
                        {
                            foreach (Component c in root.Container.Components)
                            {
                                if (c is XmppStream)
                                {
                                    m_xmpp = (XmppStream)c;
                                    break;
                                }
                            }
                        }
                    }
                }
                return m_xmpp;
            }
            set
            {
                m_xmpp = value;
                ReadXmpp();
            }
        }

        private void WriteValues(Control parent)
        {
            if (parent.Tag != null)
            {
                m_xmpp[(string)parent.Tag] = GetControlValue(parent);
            }
            if (parent.HasChildren)
            {
                foreach (Control child in parent.Controls)
                {
                    WriteValues(child);
                }
            }
        }

        /// <summary>
        /// Write to the XmppStream the current values.
        /// </summary>
        protected void WriteXmpp()
        {
            WriteValues(this);
        }

        private void WriteElem(XmlElement root, Control c)
        {
            if (c.Tag != null)
            {
                root.AppendChild(root.OwnerDocument.CreateElement((string)c.Tag)).InnerText =
                    GetControlValue(c).ToString();
            }
            if (c.HasChildren)
            {
                foreach (Control child in c.Controls)
                {
                    WriteElem(root, child);
                }
            }
        }

        /// <summary>
        /// Write the current connection properties to an XML config file.
        /// TODO: Replace this with a better ConfigFile implementation that can write.
        /// </summary>
        /// <param name="file"></param>
        public void WriteToFile(string file)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = (XmlElement)doc.CreateElement(m_xmpp.GetType().Name);
            doc.AppendChild(root);

            WriteElem(root, this);

            foreach (DictionaryEntry ent in m_extra)
            {
                root.AppendChild(doc.CreateElement((string)ent.Key)).InnerText = ent.Value.ToString();
            }

            XmlTextWriter xw = new XmlTextWriter(file, System.Text.Encoding.UTF8);
            xw.Formatting = Formatting.Indented;
            doc.WriteContentTo(xw);
            xw.Close();
        }

        private void ReadControls(Control parent)
        {
            if (parent.Tag != null)
                SetControlValue(parent, m_xmpp[(string)parent.Tag]);
            if (parent.HasChildren)
            {
                foreach (Control child in parent.Controls)
                {
                    ReadControls(child);
                }
            }
        }

        /// <summary>
        /// Read current values from the XmppStream
        /// </summary>
        protected void ReadXmpp()
        {
            ReadControls(this);
        }

        /// <summary>
        /// Set the connection properties from an XML config file.
        /// TODO: Replace this with a better ConfigFile implementation that can write.
        /// </summary>
        /// <param name="file"></param>
        public void ReadFromFile(string file)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(file);
            }
            catch (XmlException)
            {
                return;
            }
            catch (System.IO.FileNotFoundException)
            {
                return;
            }

            XmlElement root = doc.DocumentElement;
            if (root == null)
                return;
            foreach (XmlNode node in root.ChildNodes)
            {
                XmlElement elem = node as XmlElement;
                if (elem == null)
                    continue;
                try
                {
                    this[elem.Name] = elem.InnerText;
                }
                catch (ArgumentException)
                {
                    // ignored
                }
            }
            WriteXmpp();
        }

        private Control FindComponentByTag(Control parent, string tag)
        {
            if ((string)parent.Tag == tag)
                return parent;
            if (parent.HasChildren)
            {
                foreach (Control c in parent.Controls)
                {
                    Control possible = FindComponentByTag(c, tag);
                    if (possible != null)
                        return possible;
                }
            }
            return null;
        }

        private object GetControlValue(Control c)
        {
            if (c == null)
                return null;
            CheckBox chk = c as CheckBox;
            if (chk != null)
                return chk.Checked;
            TextBox txt = c as TextBox;
            if (txt != null)
                return txt.Text;
            ComboBox cmb = c as ComboBox;
            if (cmb != null)
                return cmb.SelectedItem;
            NumericUpDown num = c as NumericUpDown;
            if (num != null)
                return (int)num.Value;
            throw new ArgumentException("Control with no tag", c.Name);
        }

        private void SetControlValue(Control c, object val)
        {
            CheckBox chk = c as CheckBox;
            if (chk != null)
            {
                if (val is bool)
                    chk.Checked = (bool)val;
                else if (val is string)
                    chk.Checked = bool.Parse((string)val);
                return;
            }
            TextBox txt = c as TextBox;
            if (txt != null)
            {
                txt.Text = (string)val;
                return;
            }
            ComboBox cmb = c as ComboBox;
            if (cmb != null)
            {
                if (cmb.SelectedItem.GetType().IsAssignableFrom(val.GetType()))
                    cmb.SelectedItem = val;
                else if (val is string)
                {
                    cmb.SelectedItem = Enum.Parse(cmb.SelectedItem.GetType(), (string)val);
                }
                return;
            }
            NumericUpDown num = c as NumericUpDown;
            if (num != null)
            {
                if (val is int)
                    num.Value = (int)val;
                else if (val is string)
                {
                    num.Value = int.Parse((string)val);
                }

                return;
            }
        }

        /// <summary>
        /// Set/Get the value of an option, as it currently exists in a control.
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public object this[string option]
        {
            get
            {
                Control c = FindComponentByTag(this, option);
                if (c == null)
                {
                    if (m_extra.Contains(option))
                        return m_extra[option];
                    return null;
                }
                return GetControlValue(c);
            }
            set
            {
                Control c = FindComponentByTag(this, option);
                if (c == null)
                {
                    //throw new ArgumentException("Unknown option", option);
                    m_extra[option] = value;
                }
                else
                    SetControlValue(c, value);
            }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.tip = new System.Windows.Forms.ToolTip(this.components);
            this.error = new System.Windows.Forms.ErrorProvider();
            this.panel1.SuspendLayout();
#if NET20
            ((System.ComponentModel.ISupportInitialize)(this.error)).BeginInit();
#endif
            this.SuspendLayout();
            //
            // btnCancel
            //
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.CausesValidation = false;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(228, 8);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(48, 23);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            //
            // btnOK
            //
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Location = new System.Drawing.Point(172, 8);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(48, 23);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "OK";
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            //
            // panel1
            //
            this.panel1.CausesValidation = false;
            this.panel1.Controls.Add(this.btnCancel);
            this.panel1.Controls.Add(this.btnOK);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 226);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(292, 40);
            this.panel1.TabIndex = 1000;
            //
            // error
            //
            this.error.ContainerControl = this;
            //
            // OptionForm
            //
            this.AcceptButton = this.btnOK;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(292, 266);
            this.Controls.Add(this.panel1);
            this.Name = "OptionForm";
            this.Text = "OptionForm";
            this.Load += new System.EventHandler(this.OptionForm_Load);
            this.panel1.ResumeLayout(false);
#if NET20
            ((System.ComponentModel.ISupportInitialize)(this.error)).EndInit();
#endif
            this.ResumeLayout(false);

        }
        #endregion

        /// <summary>
        /// This field is required.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Required(object sender, System.ComponentModel.CancelEventArgs e)
        {
            TextBox box = (TextBox)sender;
            if ((box.Text == null) || (box.Text == ""))
            {
                e.Cancel = true;
                error.SetError(box, "Required");
            }
        }

        /// <summary>
        /// Clear any error blinkies.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ClearError(object sender, System.EventArgs e)
        {
            error.SetError((Control)sender, "");
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (!this.Validate())
                return;

            WriteXmpp();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void OptionForm_Load(object sender, EventArgs e)
        {
            ReadXmpp();
        }
    }
}
