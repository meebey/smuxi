namespace Smuxi.Frontend.Swf
{
	public partial class ChatView
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

		#region Component Designer generated code
        

		/// <summary>
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this._OutputTextView = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // _OutputTextView
            // 
            this._OutputTextView.DetectUrls = false;
            this._OutputTextView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._OutputTextView.Location = new System.Drawing.Point(0, 0);
            this._OutputTextView.Name = "_OutputTextView";
            this._OutputTextView.ReadOnly = true;
            this._OutputTextView.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedVertical;
            this._OutputTextView.ShowSelectionMargin = true;
            this._OutputTextView.Size = new System.Drawing.Size(100, 96);
            this._OutputTextView.TabIndex = 0;
            this._OutputTextView.Text = "";
            this.ResumeLayout(false);

		}

		#endregion
	}
}
