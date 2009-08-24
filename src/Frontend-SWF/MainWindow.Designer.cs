namespace Smuxi.Frontend.Swf
{
    partial class MainWindow
    {
    	/// <summary>
    	/// Required designer variable.
    	/// </summary>
    	private System.ComponentModel.IContainer components = null;

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
            System.Windows.Forms.MenuStrip menuStrip1;
            System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
            System.Windows.Forms.ToolStripMenuItem preferencesToolStripMenuItem;
            System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
            System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
            System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
            System.Windows.Forms.ToolStripMenuItem caretModeToolStripMenuItem;
            System.Windows.Forms.ToolStripMenuItem engineToolStripMenuItem;
            System.Windows.Forms.ToolStripMenuItem useLocalEngineToolStripMenuItem;
            System.Windows.Forms.ToolStripMenuItem addRemoteEngineToolStripMenuItem;
            System.Windows.Forms.ToolStripMenuItem switchRemoteEngineToolStripMenuItem;
            System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
            System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this._Statusbar = new System.Windows.Forms.ToolStripStatusLabel();
            this._NetworkStatusbar = new System.Windows.Forms.ToolStripStatusLabel();
            this._ProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this._Notebook = new Smuxi.Frontend.Swf.Notebook();
            this._Entry = new Smuxi.Frontend.Swf.Entry();
            menuStrip1 = new System.Windows.Forms.MenuStrip();
            fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            preferencesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            caretModeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            engineToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            useLocalEngineToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            addRemoteEngineToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            switchRemoteEngineToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            menuStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            fileToolStripMenuItem,
            viewToolStripMenuItem,
            engineToolStripMenuItem,
            helpToolStripMenuItem});
            menuStrip1.Location = new System.Drawing.Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new System.Drawing.Size(784, 24);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            preferencesToolStripMenuItem,
            toolStripMenuItem1,
            exitToolStripMenuItem});
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            fileToolStripMenuItem.Text = "&File";
            // 
            // preferencesToolStripMenuItem
            // 
            preferencesToolStripMenuItem.Name = "preferencesToolStripMenuItem";
            preferencesToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            preferencesToolStripMenuItem.Text = "&Preferences...";
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new System.Drawing.Size(149, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Q)));
            exitToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            exitToolStripMenuItem.Text = "E&xit";
            // 
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            caretModeToolStripMenuItem});
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            viewToolStripMenuItem.Text = "&View";
            // 
            // caretModeToolStripMenuItem
            // 
            caretModeToolStripMenuItem.CheckOnClick = true;
            caretModeToolStripMenuItem.Name = "caretModeToolStripMenuItem";
            caretModeToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F7;
            caretModeToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            caretModeToolStripMenuItem.Text = "&Caret Mode";
            // 
            // engineToolStripMenuItem
            // 
            engineToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            useLocalEngineToolStripMenuItem,
            addRemoteEngineToolStripMenuItem,
            switchRemoteEngineToolStripMenuItem});
            engineToolStripMenuItem.Name = "engineToolStripMenuItem";
            engineToolStripMenuItem.Size = new System.Drawing.Size(55, 20);
            engineToolStripMenuItem.Text = "&Engine";
            // 
            // useLocalEngineToolStripMenuItem
            // 
            useLocalEngineToolStripMenuItem.Name = "useLocalEngineToolStripMenuItem";
            useLocalEngineToolStripMenuItem.Size = new System.Drawing.Size(192, 22);
            useLocalEngineToolStripMenuItem.Text = "&Use Local Engine";
            // 
            // addRemoteEngineToolStripMenuItem
            // 
            addRemoteEngineToolStripMenuItem.Name = "addRemoteEngineToolStripMenuItem";
            addRemoteEngineToolStripMenuItem.Size = new System.Drawing.Size(192, 22);
            addRemoteEngineToolStripMenuItem.Text = "&Add Remote Engine";
            // 
            // switchRemoteEngineToolStripMenuItem
            // 
            switchRemoteEngineToolStripMenuItem.Name = "switchRemoteEngineToolStripMenuItem";
            switchRemoteEngineToolStripMenuItem.Size = new System.Drawing.Size(192, 22);
            switchRemoteEngineToolStripMenuItem.Text = "&Switch Remote Engine";
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            aboutToolStripMenuItem});
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            helpToolStripMenuItem.Text = "&Help";
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            aboutToolStripMenuItem.Text = "&About";
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._NetworkStatusbar,
            this._Statusbar,
            this._ProgressBar});
            this.statusStrip1.Location = new System.Drawing.Point(0, 540);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(784, 24);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // _Statusbar
            // 
            this._Statusbar.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top)
                        | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)
                        | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
            this._Statusbar.BorderStyle = System.Windows.Forms.Border3DStyle.SunkenInner;
            this._Statusbar.Name = "_Statusbar";
            this._Statusbar.Size = new System.Drawing.Size(318, 19);
            this._Statusbar.Spring = true;
            this._Statusbar.Text = "Status";
            this._Statusbar.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _NetworkStatusbar
            // 
            this._NetworkStatusbar.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top)
                        | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)
                        | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
            this._NetworkStatusbar.BorderStyle = System.Windows.Forms.Border3DStyle.SunkenInner;
            this._NetworkStatusbar.Name = "_NetworkStatusbar";
            this._NetworkStatusbar.Size = new System.Drawing.Size(318, 19);
            this._NetworkStatusbar.Spring = true;
            this._NetworkStatusbar.Text = "NetworkStatus";
            this._NetworkStatusbar.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _ProgressBar
            // 
            this._ProgressBar.Name = "_ProgressBar";
            this._ProgressBar.Size = new System.Drawing.Size(100, 18);
            // 
            // _Notebook
            // 
            this._Notebook.Dock = System.Windows.Forms.DockStyle.Fill;
            this._Notebook.Location = new System.Drawing.Point(0, 24);
            this._Notebook.Name = "_Notebook";
            this._Notebook.SelectedIndex = 0;
            this._Notebook.Size = new System.Drawing.Size(784, 496);
            this._Notebook.TabIndex = 2;
            // 
            // _Entry
            // 
            this._Entry.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._Entry.Location = new System.Drawing.Point(0, 520);
            this._Entry.Name = "_Entry";
            this._Entry.Size = new System.Drawing.Size(784, 20);
            this._Entry.TabIndex = 3;
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 564);
            this.Controls.Add(this._Notebook);
            this.Controls.Add(this._Entry);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(menuStrip1);
            this.MainMenuStrip = menuStrip1;
            this.Name = "MainWindow";
            this.Text = "Smuxi - Smart MUltipleXed Irc";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel _Statusbar;
        private System.Windows.Forms.ToolStripStatusLabel _NetworkStatusbar;
        private System.Windows.Forms.ToolStripProgressBar _ProgressBar;
    }
}