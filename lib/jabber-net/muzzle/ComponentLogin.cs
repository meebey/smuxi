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
using System.Windows.Forms;
using System.Xml;

using jabber.connection;
using jabber.server;

namespace muzzle
{
    /// <summary>
    /// A login form for client connections.
    /// </summary>
    /// <example>
    /// ComponentLogin l = new ComponentLogin(jc);
    ///
    /// if (l.ShowDialog(this) == DialogResult.OK)
    /// {
    ///     jc.Connect();
    /// }
    /// </example>
    public class ComponentLogin : OptionForm
    {

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtUser;
        private System.Windows.Forms.TextBox txtServer;
        private System.Windows.Forms.NumericUpDown numPort;
        private System.Windows.Forms.TextBox txtPass;
        private ComboBox cmbType;
        private Label label5;

        /// <summary>
        /// Create a Client Login dialog box
        /// </summary>
        public ComponentLogin()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            for (ComponentType ct=ComponentType.Accept; ct <= ComponentType.Connect; ct++)
            {
                cmbType.Items.Add(ct);
            }
            cmbType.SelectedItem = ComponentType.Accept;

            txtUser.Tag = Options.TO;
            txtServer.Tag = Options.NETWORK_HOST;
            numPort.Tag = Options.PORT;
            txtPass.Tag = Options.PASSWORD;
            cmbType.Tag = Options.COMPONENT_DIRECTION;
        }

        /// <summary>
        /// Create a Client Login dialog box that manages a component
        /// </summary>
        /// <param name="service">The component to manage</param>
        public ComponentLogin(jabber.server.JabberService service) : this()
        {
            this.Xmpp = service;
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txtUser = new System.Windows.Forms.TextBox();
            this.txtServer = new System.Windows.Forms.TextBox();
            this.numPort = new System.Windows.Forms.NumericUpDown();
            this.txtPass = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.cmbType = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.error)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPort)).BeginInit();
            this.SuspendLayout();
            //
            // label1
            //
            this.label1.Location = new System.Drawing.Point(8, 7);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(48, 23);
            this.label1.TabIndex = 0;
            this.label1.Text = "Host:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // label2
            //
            this.label2.Location = new System.Drawing.Point(8, 63);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(48, 23);
            this.label2.TabIndex = 4;
            this.label2.Text = "ID:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // label3
            //
            this.label3.Location = new System.Drawing.Point(8, 35);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(48, 23);
            this.label3.TabIndex = 2;
            this.label3.Text = "Port:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtUser
            //
            this.txtUser.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtUser.Location = new System.Drawing.Point(56, 64);
            this.txtUser.Name = "txtUser";
            this.txtUser.Size = new System.Drawing.Size(220, 20);
            this.txtUser.TabIndex = 5;
            this.tip.SetToolTip(this.txtUser, "Service ID for this component");
            this.txtUser.Validated += new System.EventHandler(this.onValidated);
            this.txtUser.Validating += new System.ComponentModel.CancelEventHandler(this.Required_Validating);
            //
            // txtServer
            //
            this.txtServer.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtServer.Location = new System.Drawing.Point(56, 8);
            this.txtServer.Name = "txtServer";
            this.txtServer.Size = new System.Drawing.Size(220, 20);
            this.txtServer.TabIndex = 1;
            this.tip.SetToolTip(this.txtServer, "DNS name or IP address of router to connect to.  Not required if in Listen mode.");
            this.txtServer.Validated += new System.EventHandler(this.onValidated);
            this.txtServer.Validating += new System.ComponentModel.CancelEventHandler(this.Required_Validating);
            //
            // numPort
            //
            this.numPort.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.numPort.Location = new System.Drawing.Point(56, 36);
            this.numPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.numPort.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numPort.Name = "numPort";
            this.numPort.Size = new System.Drawing.Size(220, 20);
            this.numPort.TabIndex = 3;
            this.tip.SetToolTip(this.numPort, "TCP port to connect to, or port to listen on if in Listen mode.");
            this.numPort.Value = new decimal(new int[] {
            7400,
            0,
            0,
            0});
            //
            // txtPass
            //
            this.txtPass.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPass.Location = new System.Drawing.Point(56, 92);
            this.txtPass.Name = "txtPass";
            this.txtPass.PasswordChar = '*';
            this.txtPass.Size = new System.Drawing.Size(220, 20);
            this.txtPass.TabIndex = 7;
            this.tip.SetToolTip(this.txtPass, "Secret shared with router");
            this.txtPass.Validated += new System.EventHandler(this.onValidated);
            this.txtPass.Validating += new System.ComponentModel.CancelEventHandler(this.Required_Validating);
            //
            // label4
            //
            this.label4.Location = new System.Drawing.Point(8, 91);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(48, 23);
            this.label4.TabIndex = 6;
            this.label4.Text = "Secret:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // cmbType
            //
            this.cmbType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbType.Location = new System.Drawing.Point(56, 118);
            this.cmbType.Name = "cmbType";
            this.cmbType.Size = new System.Drawing.Size(221, 21);
            this.cmbType.TabIndex = 9;
            //
            // label5
            //
            this.label5.Location = new System.Drawing.Point(8, 116);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(48, 23);
            this.label5.TabIndex = 8;
            this.label5.Text = "Type:";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // ComponentLogin
            //
            this.ClientSize = new System.Drawing.Size(292, 266);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.txtPass);
            this.Controls.Add(this.cmbType);
            this.Controls.Add(this.numPort);
            this.Controls.Add(this.txtServer);
            this.Controls.Add(this.txtUser);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "ComponentLogin";
            this.Text = "Connection";
            this.Controls.SetChildIndex(this.label1, 0);
            this.Controls.SetChildIndex(this.label2, 0);
            this.Controls.SetChildIndex(this.label3, 0);
            this.Controls.SetChildIndex(this.label4, 0);
            this.Controls.SetChildIndex(this.txtUser, 0);
            this.Controls.SetChildIndex(this.txtServer, 0);
            this.Controls.SetChildIndex(this.numPort, 0);
            this.Controls.SetChildIndex(this.cmbType, 0);
            this.Controls.SetChildIndex(this.txtPass, 0);
            this.Controls.SetChildIndex(this.label5, 0);
            ((System.ComponentModel.ISupportInitialize)(this.error)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPort)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
#endregion

        private void Required_Validating(object sender, CancelEventArgs e)
        {
            this.Required(sender, e);
        }

        private void onValidated(object sender, EventArgs e)
        {
            this.ClearError(sender, e);
        }
    }
}
