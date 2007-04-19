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
using System.ComponentModel.Design;
using System.Windows.Forms;
using System.Xml;

using bedrock.util;
using jabber.connection;

namespace muzzle
{
    /// <summary>
    /// A login form for client connections.
    /// </summary>
    /// <example>
    /// ClientLogin l = new ClientLogin(jc);
    ///
    /// if (l.ShowDialog(this) == DialogResult.OK)
    /// {
    ///     jc.Connect();
    /// }
    /// </example>
    [SVN(@"$Id: ClientLogin.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class ClientLogin : OptionForm
    {
        private System.Windows.Forms.CheckBox cbSSL;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tpBasic;
        private System.Windows.Forms.TextBox txtPass;
        private System.Windows.Forms.TextBox txtServer;
        private System.Windows.Forms.TextBox txtUser;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TabPage tpNetwork;
        private System.Windows.Forms.NumericUpDown numPort;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtNetworkHost;
        private System.Windows.Forms.TabPage tpProxy;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cmbProxy;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.NumericUpDown numProxyPort;
        private System.Windows.Forms.TextBox txtProxyUser;
        private System.Windows.Forms.TextBox txtProxyPassword;
        private System.Windows.Forms.TextBox txtProxyHost;
        private System.Windows.Forms.CheckBox cbPlaintext;
        private TabPage tpConnection;
        private TextBox txtURL;
        private Label label12;
        private ComboBox cmbConnectionType;
        private Label label11;

        /// <summary>
        /// Create a Client Login dialog box
        /// </summary>
        public ClientLogin() : base()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();
#if NO_SSL
            cbSSL.Visible = false;
#endif

            for (ProxyType pt=ProxyType.None; pt <= ProxyType.CONNECT; pt++)
            {
                cmbProxy.Items.Add(pt);
            }
            cmbProxy.SelectedItem = ProxyType.None;

            for (ConnectionType ct=ConnectionType.Socket; ct <= ConnectionType.HTTP_Binding; ct++)
            {
                cmbConnectionType.Items.Add(ct);
            }
            cmbConnectionType.SelectedItem = ConnectionType.Socket;

            cbSSL.Tag = Options.SSL;
            txtPass.Tag = Options.PASSWORD;
            txtServer.Tag = Options.TO;
            txtUser.Tag = Options.USER;
            numPort.Tag = Options.PORT;
            txtNetworkHost.Tag = Options.NETWORK_HOST;
            cmbProxy.Tag = Options.PROXY_TYPE;
            numProxyPort.Tag = Options.PROXY_PORT;
            txtProxyUser.Tag = Options.PROXY_USER;
            txtProxyPassword.Tag = Options.PROXY_PW;
            txtProxyHost.Tag = Options.PROXY_HOST;
            cbPlaintext.Tag = Options.PLAINTEXT;
            txtURL.Tag = Options.POLL_URL;
            cmbConnectionType.Tag = Options.CONNECTION_TYPE;
        }

        /// <summary>
        /// Create a Client Login dialog box than manages the connection properties of a particular client
        /// connection.
        /// </summary>
        /// <param name="cli">The client connection to modify</param>
        public ClientLogin(jabber.client.JabberClient cli) : this()
        {
            this.Xmpp = cli;
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
            this.txtServer = new System.Windows.Forms.TextBox();
            this.txtUser = new System.Windows.Forms.TextBox();
            this.cbSSL = new System.Windows.Forms.CheckBox();
            this.numPort = new System.Windows.Forms.NumericUpDown();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tpBasic = new System.Windows.Forms.TabPage();
            this.cbPlaintext = new System.Windows.Forms.CheckBox();
            this.txtPass = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.tpNetwork = new System.Windows.Forms.TabPage();
            this.txtNetworkHost = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.tpConnection = new System.Windows.Forms.TabPage();
            this.txtURL = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.cmbConnectionType = new System.Windows.Forms.ComboBox();
            this.label11 = new System.Windows.Forms.Label();
            this.tpProxy = new System.Windows.Forms.TabPage();
            this.txtProxyPassword = new System.Windows.Forms.TextBox();
            this.txtProxyUser = new System.Windows.Forms.TextBox();
            this.numProxyPort = new System.Windows.Forms.NumericUpDown();
            this.txtProxyHost = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.cmbProxy = new System.Windows.Forms.ComboBox();
#if NET20
            ((System.ComponentModel.ISupportInitialize)(this.error)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPort)).BeginInit();
#endif
            this.tabControl1.SuspendLayout();
            this.tpBasic.SuspendLayout();
            this.tpNetwork.SuspendLayout();
            this.tpConnection.SuspendLayout();
            this.tpProxy.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numProxyPort)).BeginInit();
            this.SuspendLayout();
            //
            // txtServer
            //
            this.txtServer.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtServer.Location = new System.Drawing.Point(72, 72);
            this.txtServer.Name = "txtServer";
            this.txtServer.Size = new System.Drawing.Size(191, 20);
            this.txtServer.TabIndex = 5;
            this.tip.SetToolTip(this.txtServer, "The name of the Jabber server");
            this.txtServer.Validated += new System.EventHandler(this.onValidated);
            this.txtServer.Validating += new System.ComponentModel.CancelEventHandler(this.Required_Validating);
            //
            // txtUser
            //
            this.txtUser.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtUser.Location = new System.Drawing.Point(72, 8);
            this.txtUser.Name = "txtUser";
            this.txtUser.Size = new System.Drawing.Size(191, 20);
            this.txtUser.TabIndex = 1;
            this.txtUser.Tag = "";
            this.tip.SetToolTip(this.txtUser, "The user portion of the JID only.");
            this.txtUser.Validated += new System.EventHandler(this.onValidated);
            this.txtUser.Validating += new System.ComponentModel.CancelEventHandler(this.Required_Validating);
            //
            // cbSSL
            //
            this.cbSSL.AccessibleDescription = "";
            this.cbSSL.Location = new System.Drawing.Point(8, 64);
            this.cbSSL.Name = "cbSSL";
            this.cbSSL.Size = new System.Drawing.Size(48, 24);
            this.cbSSL.TabIndex = 4;
            this.cbSSL.Text = "SSL";
            this.tip.SetToolTip(this.cbSSL, "Connect using Secure Socket Layer encryption");
            this.cbSSL.CheckedChanged += new System.EventHandler(this.cbSSL_CheckedChanged);
            //
            // numPort
            //
            this.numPort.Location = new System.Drawing.Point(88, 10);
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
            this.numPort.Size = new System.Drawing.Size(94, 20);
            this.numPort.TabIndex = 1;
            this.tip.SetToolTip(this.numPort, "TCP port to connect on");
            this.numPort.Value = new decimal(new int[] {
            5222,
            0,
            0,
            0});
            //
            // tabControl1
            //
            this.tabControl1.Controls.Add(this.tpBasic);
            this.tabControl1.Controls.Add(this.tpNetwork);
            this.tabControl1.Controls.Add(this.tpConnection);
            this.tabControl1.Controls.Add(this.tpProxy);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(292, 226);
            this.tabControl1.TabIndex = 0;
            //
            // tpBasic
            //
            this.tpBasic.Controls.Add(this.cbPlaintext);
            this.tpBasic.Controls.Add(this.txtPass);
            this.tpBasic.Controls.Add(this.txtServer);
            this.tpBasic.Controls.Add(this.txtUser);
            this.tpBasic.Controls.Add(this.label4);
            this.tpBasic.Controls.Add(this.label2);
            this.tpBasic.Controls.Add(this.label1);
            this.tpBasic.Location = new System.Drawing.Point(4, 22);
            this.tpBasic.Name = "tpBasic";
            this.tpBasic.Size = new System.Drawing.Size(284, 200);
            this.tpBasic.TabIndex = 0;
            this.tpBasic.Text = "Basic";
            //
            // cbPlaintext
            //
            this.cbPlaintext.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbPlaintext.Location = new System.Drawing.Point(8, 104);
            this.cbPlaintext.Name = "cbPlaintext";
            this.cbPlaintext.Size = new System.Drawing.Size(268, 32);
            this.cbPlaintext.TabIndex = 6;
            this.cbPlaintext.Text = "Allow plaintext authentication";
            //
            // txtPass
            //
            this.txtPass.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPass.Location = new System.Drawing.Point(72, 40);
            this.txtPass.Name = "txtPass";
            this.txtPass.PasswordChar = '*';
            this.txtPass.Size = new System.Drawing.Size(191, 20);
            this.txtPass.TabIndex = 3;
            this.txtPass.Validated += new System.EventHandler(this.onValidated);
            this.txtPass.Validating += new System.ComponentModel.CancelEventHandler(this.Required_Validating);
            //
            // label4
            //
            this.label4.Location = new System.Drawing.Point(8, 39);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(64, 23);
            this.label4.TabIndex = 2;
            this.label4.Text = "Password:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // label2
            //
            this.label2.Location = new System.Drawing.Point(8, 71);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(48, 23);
            this.label2.TabIndex = 4;
            this.label2.Text = "Server:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // label1
            //
            this.label1.Location = new System.Drawing.Point(8, 7);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(48, 23);
            this.label1.TabIndex = 0;
            this.label1.Text = "User:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // tpNetwork
            //
            this.tpNetwork.Controls.Add(this.txtNetworkHost);
            this.tpNetwork.Controls.Add(this.label5);
            this.tpNetwork.Controls.Add(this.cbSSL);
            this.tpNetwork.Controls.Add(this.numPort);
            this.tpNetwork.Controls.Add(this.label3);
            this.tpNetwork.Location = new System.Drawing.Point(4, 22);
            this.tpNetwork.Name = "tpNetwork";
            this.tpNetwork.Size = new System.Drawing.Size(284, 200);
            this.tpNetwork.TabIndex = 2;
            this.tpNetwork.Text = "Network";
            //
            // txtNetworkHost
            //
            this.txtNetworkHost.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtNetworkHost.Location = new System.Drawing.Point(88, 37);
            this.txtNetworkHost.Name = "txtNetworkHost";
            this.txtNetworkHost.Size = new System.Drawing.Size(173, 20);
            this.txtNetworkHost.TabIndex = 3;
            //
            // label5
            //
            this.label5.Location = new System.Drawing.Point(8, 36);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(80, 23);
            this.label5.TabIndex = 2;
            this.label5.Text = "Network Host:";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // label3
            //
            this.label3.Location = new System.Drawing.Point(8, 9);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(80, 23);
            this.label3.TabIndex = 0;
            this.label3.Text = "Port:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // tpConnection
            //
            this.tpConnection.Controls.Add(this.txtURL);
            this.tpConnection.Controls.Add(this.label12);
            this.tpConnection.Controls.Add(this.cmbConnectionType);
            this.tpConnection.Controls.Add(this.label11);
            this.tpConnection.Location = new System.Drawing.Point(4, 22);
            this.tpConnection.Name = "tpConnection";
            this.tpConnection.Size = new System.Drawing.Size(284, 200);
            this.tpConnection.TabIndex = 3;
            this.tpConnection.Text = "Connection";
            //
            // txtURL
            //
            this.txtURL.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtURL.Enabled = false;
            this.txtURL.Location = new System.Drawing.Point(50, 40);
            this.txtURL.Name = "txtURL";
            this.txtURL.Size = new System.Drawing.Size(170, 20);
            this.txtURL.TabIndex = 3;
            //
            // label12
            //
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(14, 43);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(32, 13);
            this.label12.TabIndex = 2;
            this.label12.Text = "URL:";
            //
            // cmbConnectionType
            //
            this.cmbConnectionType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbConnectionType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbConnectionType.Location = new System.Drawing.Point(50, 9);
            this.cmbConnectionType.Name = "cmbConnectionType";
            this.cmbConnectionType.Size = new System.Drawing.Size(170, 21);
            this.cmbConnectionType.TabIndex = 1;
            this.cmbConnectionType.SelectedIndexChanged += new System.EventHandler(this.cmbConnectionType_SelectedIndexChanged);
            //
            // label11
            //
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(10, 12);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(34, 13);
            this.label11.TabIndex = 0;
            this.label11.Text = "Type:";
            //
            // tpProxy
            //
            this.tpProxy.Controls.Add(this.txtProxyPassword);
            this.tpProxy.Controls.Add(this.txtProxyUser);
            this.tpProxy.Controls.Add(this.numProxyPort);
            this.tpProxy.Controls.Add(this.txtProxyHost);
            this.tpProxy.Controls.Add(this.label10);
            this.tpProxy.Controls.Add(this.label9);
            this.tpProxy.Controls.Add(this.label8);
            this.tpProxy.Controls.Add(this.label7);
            this.tpProxy.Controls.Add(this.label6);
            this.tpProxy.Controls.Add(this.cmbProxy);
            this.tpProxy.Location = new System.Drawing.Point(4, 22);
            this.tpProxy.Name = "tpProxy";
            this.tpProxy.Size = new System.Drawing.Size(284, 200);
            this.tpProxy.TabIndex = 1;
            this.tpProxy.Text = "Proxy";
            //
            // txtProxyPassword
            //
            this.txtProxyPassword.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProxyPassword.Enabled = false;
            this.txtProxyPassword.Location = new System.Drawing.Point(72, 117);
            this.txtProxyPassword.Name = "txtProxyPassword";
            this.txtProxyPassword.PasswordChar = '*';
            this.txtProxyPassword.Size = new System.Drawing.Size(140, 20);
            this.txtProxyPassword.TabIndex = 9;
            //
            // txtProxyUser
            //
            this.txtProxyUser.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProxyUser.Enabled = false;
            this.txtProxyUser.Location = new System.Drawing.Point(72, 90);
            this.txtProxyUser.Name = "txtProxyUser";
            this.txtProxyUser.Size = new System.Drawing.Size(140, 20);
            this.txtProxyUser.TabIndex = 7;
            //
            // numProxyPort
            //
            this.numProxyPort.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.numProxyPort.Enabled = false;
            this.numProxyPort.Location = new System.Drawing.Point(72, 63);
            this.numProxyPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.numProxyPort.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numProxyPort.Name = "numProxyPort";
            this.numProxyPort.Size = new System.Drawing.Size(140, 20);
            this.numProxyPort.TabIndex = 5;
            this.numProxyPort.Value = new decimal(new int[] {
            1080,
            0,
            0,
            0});
            //
            // txtProxyHost
            //
            this.txtProxyHost.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProxyHost.Enabled = false;
            this.txtProxyHost.Location = new System.Drawing.Point(72, 36);
            this.txtProxyHost.Name = "txtProxyHost";
            this.txtProxyHost.Size = new System.Drawing.Size(140, 20);
            this.txtProxyHost.TabIndex = 3;
            //
            // label10
            //
            this.label10.Location = new System.Drawing.Point(8, 116);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(64, 23);
            this.label10.TabIndex = 8;
            this.label10.Text = "Password:";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // label9
            //
            this.label9.Location = new System.Drawing.Point(8, 89);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(64, 23);
            this.label9.TabIndex = 6;
            this.label9.Text = "User:";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // label8
            //
            this.label8.Location = new System.Drawing.Point(8, 62);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(64, 23);
            this.label8.TabIndex = 4;
            this.label8.Text = "Port:";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // label7
            //
            this.label7.Location = new System.Drawing.Point(8, 35);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(64, 23);
            this.label7.TabIndex = 2;
            this.label7.Text = "Server:";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // label6
            //
            this.label6.Location = new System.Drawing.Point(8, 7);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(64, 23);
            this.label6.TabIndex = 0;
            this.label6.Text = "Type:";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // cmbProxy
            //
            this.cmbProxy.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbProxy.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbProxy.Location = new System.Drawing.Point(72, 8);
            this.cmbProxy.Name = "cmbProxy";
            this.cmbProxy.Size = new System.Drawing.Size(140, 21);
            this.cmbProxy.TabIndex = 1;
            this.cmbProxy.SelectedIndexChanged += new System.EventHandler(this.cmbProxy_SelectedIndexChanged);
            //
            // ClientLogin
            //
            this.ClientSize = new System.Drawing.Size(292, 266);
            this.Controls.Add(this.tabControl1);
            this.MinimizeBox = false;
            this.Name = "ClientLogin";
            this.Text = "Login";
            this.Controls.SetChildIndex(this.tabControl1, 0);
#if NET20
            ((System.ComponentModel.ISupportInitialize)(this.error)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPort)).EndInit();
#endif
            this.tabControl1.ResumeLayout(false);
            this.tpBasic.ResumeLayout(false);
            this.tpBasic.PerformLayout();
            this.tpNetwork.ResumeLayout(false);
            this.tpNetwork.PerformLayout();
            this.tpConnection.ResumeLayout(false);
            this.tpConnection.PerformLayout();
            this.tpProxy.ResumeLayout(false);
            this.tpProxy.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numProxyPort)).EndInit();
            this.ResumeLayout(false);

        }
        #endregion


        private void cbSSL_CheckedChanged(object sender, System.EventArgs e)
        {
            if (cbSSL.Checked)
            {
                if (numPort.Value == 5222)
                    numPort.Value = 5223;
            }
            else
            {
                if (numPort.Value == 5223)
                    numPort.Value = 5222;
            }
        }

        private void cmbConnectionType_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool socket = (cmbConnectionType.SelectedIndex == 0);
            bool prox = (cmbProxy.SelectedIndex != 0);
            txtURL.Enabled = !socket;
            cmbProxy.Enabled = socket;
            txtProxyHost.Enabled = socket && prox;
            numProxyPort.Enabled = socket && prox;
            txtProxyUser.Enabled = socket && prox;
            txtProxyPassword.Enabled = socket && prox;
        }

        private void cmbProxy_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool prox = (cmbProxy.SelectedIndex != 0);
            txtProxyHost.Enabled = prox;
            numProxyPort.Enabled = prox;
            txtProxyUser.Enabled = prox;
            txtProxyPassword.Enabled = prox;
        }

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
