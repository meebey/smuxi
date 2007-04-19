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
using System.Windows.Forms;

namespace muzzle
{
    /// <summary>
    /// Summary description for JidMulti.
    /// </summary>
    public class JidMulti : System.Windows.Forms.UserControl
    {
        private System.Windows.Forms.ListBox lstJID;
        private System.Windows.Forms.ToolTip tip;
        private System.Windows.Forms.ErrorProvider error;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.TextBox txtEntry;
        private System.ComponentModel.IContainer components;

        /// <summary>
        /// Create a JidMulti control
        /// </summary>
        public JidMulti()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();

            // TODO: Add any initialization after the InitializeComponent call

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
        /// Add a range of JIDs or strings to the list.
        /// </summary>
        /// <param name="range"></param>
        public void AddRange(object[] range)
        {
            lstJID.Items.AddRange(range);
        }

        /// <summary>
        /// Get the list of JIDs in the control currently.
        /// </summary>
        /// <returns></returns>
        public string[] GetValues()
        {
            string[] vals = new string[lstJID.Items.Count];
            for (int i=0; i < vals.Length; i++)
            {
                vals[i] = lstJID.Items[i].ToString();
            }
            return vals;
        }

        #region Component Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.lstJID = new System.Windows.Forms.ListBox();
            this.tip = new System.Windows.Forms.ToolTip(this.components);
            this.error = new System.Windows.Forms.ErrorProvider();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnRemove = new System.Windows.Forms.Button();
            this.btnAdd = new System.Windows.Forms.Button();
            this.txtEntry = new System.Windows.Forms.TextBox();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            //
            // lstJID
            //
            this.lstJID.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstJID.IntegralHeight = false;
            this.lstJID.Location = new System.Drawing.Point(0, 32);
            this.lstJID.Name = "lstJID";
            this.lstJID.Size = new System.Drawing.Size(256, 88);
            this.lstJID.Sorted = true;
            this.lstJID.TabIndex = 0;
            this.lstJID.SelectedIndexChanged += new System.EventHandler(this.lstJID_SelectedIndexChanged);
            //
            // error
            //
            this.error.ContainerControl = this;
            //
            // panel1
            //
            this.panel1.Controls.Add(this.btnRemove);
            this.panel1.Controls.Add(this.btnAdd);
            this.panel1.Controls.Add(this.txtEntry);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(256, 32);
            this.panel1.TabIndex = 4;
            //
            // btnRemove
            //
            this.btnRemove.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRemove.Location = new System.Drawing.Point(232, 5);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(24, 23);
            this.btnRemove.TabIndex = 6;
            this.btnRemove.Text = "-";
            this.tip.SetToolTip(this.btnRemove, "Remove from the list the Jabber ID on the left");
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            //
            // btnAdd
            //
            this.btnAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAdd.Location = new System.Drawing.Point(208, 5);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(24, 23);
            this.btnAdd.TabIndex = 5;
            this.btnAdd.Text = "+";
            this.tip.SetToolTip(this.btnAdd, "Add to the list the Jabber ID to the left");
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            //
            // txtEntry
            //
            this.txtEntry.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.txtEntry.Location = new System.Drawing.Point(0, 6);
            this.txtEntry.Name = "txtEntry";
            this.txtEntry.Size = new System.Drawing.Size(184, 20);
            this.txtEntry.TabIndex = 4;
            this.txtEntry.Text = "";
            this.tip.SetToolTip(this.txtEntry, "Enter a Jabber ID here, and press the + or - button to add or remove it from the " +
                "list.");
            //
            // JidMulti
            //
            this.Controls.Add(this.lstJID);
            this.Controls.Add(this.panel1);
            this.Name = "JidMulti";
            this.Size = new System.Drawing.Size(256, 120);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

        private void btnAdd_Click(object sender, System.EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                jabber.JID jid = new jabber.JID(txtEntry.Text);
                lstJID.Items.Add(jid);
                txtEntry.Clear();
                error.SetError(txtEntry, null);
            }
            catch
            {
                error.SetError(txtEntry, "Invalid JID");
            }
            this.Cursor = Cursors.Default;
        }

        private void btnRemove_Click(object sender, System.EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                jabber.JID jid = new jabber.JID(txtEntry.Text);
                int i = 0;
                foreach (object o in lstJID.Items)
                {
                    if (jid.Equals(o))
                    {
                        lstJID.Items.RemoveAt(i);
                        txtEntry.Clear();
                        error.SetError(txtEntry, null);
                        break;
                    }
                    i++;
                }
            }
            catch (Exception ex)
            {
                error.SetError(txtEntry, "Invalid JID: " + ex.ToString());
            }
            this.Cursor = Cursors.Default;
        }

        private void lstJID_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (lstJID.SelectedIndex >= 0)
                txtEntry.Text = lstJID.Items[lstJID.SelectedIndex].ToString();
        }
    }
}
