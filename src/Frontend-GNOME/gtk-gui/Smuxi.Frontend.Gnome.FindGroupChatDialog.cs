
// This file has been generated by the GUI designer. Do not modify.
namespace Smuxi.Frontend.Gnome
{
	public partial class FindGroupChatDialog
	{
		private global::Gtk.VBox vbox2;
		private global::Gtk.HBox hbox1;
		private global::Gtk.HBox hbox2;
		private global::Gtk.Label label1;
		private global::Gtk.Entry f_NameEntry;
		private global::Gtk.Button f_FindButton;
		private global::Gtk.ScrolledWindow GtkScrolledWindow;
		private global::Gtk.TreeView f_TreeView;
		private global::Gtk.Button f_CancelButton;
		private global::Gtk.Button f_OKButton;
        
		protected virtual void Build ()
		{
			global::Stetic.Gui.Initialize (this);
			// Widget Smuxi.Frontend.Gnome.FindGroupChatDialog
			this.Name = "Smuxi.Frontend.Gnome.FindGroupChatDialog";
			this.Title = global::Mono.Unix.Catalog.GetString ("Smuxi - Find Group Chat");
			this.Icon = global::Stetic.IconLoader.LoadIcon (this, "gtk-find", global::Gtk.IconSize.Menu);
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			this.DefaultWidth = 640;
			this.DefaultHeight = 480;
			// Internal child Smuxi.Frontend.Gnome.FindGroupChatDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			w1.BorderWidth = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.vbox2 = new global::Gtk.VBox ();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			// Container child vbox2.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox ();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.hbox2 = new global::Gtk.HBox ();
			this.hbox2.Name = "hbox2";
			this.hbox2.Spacing = 6;
			// Container child hbox2.Gtk.Box+BoxChild
			this.label1 = new global::Gtk.Label ();
			this.label1.Name = "label1";
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString ("_Name:");
			this.label1.UseUnderline = true;
			this.hbox2.Add (this.label1);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox2 [this.label1]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child hbox2.Gtk.Box+BoxChild
			this.f_NameEntry = new global::Gtk.Entry ();
			this.f_NameEntry.CanDefault = true;
			this.f_NameEntry.CanFocus = true;
			this.f_NameEntry.Name = "f_NameEntry";
			this.f_NameEntry.IsEditable = true;
			this.f_NameEntry.InvisibleChar = '●';
			this.hbox2.Add (this.f_NameEntry);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.hbox2 [this.f_NameEntry]));
			w3.Position = 1;
			this.hbox1.Add (this.hbox2);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.hbox1 [this.hbox2]));
			w4.Position = 0;
			// Container child hbox1.Gtk.Box+BoxChild
			this.f_FindButton = new global::Gtk.Button ();
			this.f_FindButton.CanFocus = true;
			this.f_FindButton.Name = "f_FindButton";
			this.f_FindButton.UseStock = true;
			this.f_FindButton.UseUnderline = true;
			this.f_FindButton.Label = "gtk-find";
			this.hbox1.Add (this.f_FindButton);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.hbox1 [this.f_FindButton]));
			w5.Position = 1;
			w5.Expand = false;
			w5.Fill = false;
			this.vbox2.Add (this.hbox1);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox2 [this.hbox1]));
			w6.Position = 0;
			w6.Expand = false;
			w6.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.GtkScrolledWindow = new global::Gtk.ScrolledWindow ();
			this.GtkScrolledWindow.Name = "GtkScrolledWindow";
			this.GtkScrolledWindow.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow.Gtk.Container+ContainerChild
			this.f_TreeView = new global::Gtk.TreeView ();
			this.f_TreeView.CanFocus = true;
			this.f_TreeView.Name = "f_TreeView";
			this.GtkScrolledWindow.Add (this.f_TreeView);
			this.vbox2.Add (this.GtkScrolledWindow);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox2 [this.GtkScrolledWindow]));
			w8.Position = 1;
			w1.Add (this.vbox2);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(w1 [this.vbox2]));
			w9.Position = 0;
			// Internal child Smuxi.Frontend.Gnome.FindGroupChatDialog.ActionArea
			global::Gtk.HButtonBox w10 = this.ActionArea;
			w10.Name = "dialog1_ActionArea";
			w10.Spacing = 6;
			w10.BorderWidth = ((uint)(5));
			w10.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.f_CancelButton = new global::Gtk.Button ();
			this.f_CancelButton.CanDefault = true;
			this.f_CancelButton.CanFocus = true;
			this.f_CancelButton.Name = "f_CancelButton";
			this.f_CancelButton.UseStock = true;
			this.f_CancelButton.UseUnderline = true;
			this.f_CancelButton.Label = "gtk-cancel";
			this.AddActionWidget (this.f_CancelButton, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w11 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w10 [this.f_CancelButton]));
			w11.Expand = false;
			w11.Fill = false;
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.f_OKButton = new global::Gtk.Button ();
			this.f_OKButton.CanDefault = true;
			this.f_OKButton.CanFocus = true;
			this.f_OKButton.Name = "f_OKButton";
			this.f_OKButton.UseStock = true;
			this.f_OKButton.UseUnderline = true;
			this.f_OKButton.Label = "gtk-ok";
			this.AddActionWidget (this.f_OKButton, -5);
			global::Gtk.ButtonBox.ButtonBoxChild w12 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w10 [this.f_OKButton]));
			w12.Position = 1;
			w12.Expand = false;
			w12.Fill = false;
			if ((this.Child != null)) {
				this.Child.ShowAll ();
			}
			this.label1.MnemonicWidget = this.f_NameEntry;
			this.Show ();
			this.f_NameEntry.Activated += new global::System.EventHandler (this.OnNameEntryActivated);
			this.f_FindButton.Clicked += new global::System.EventHandler (this.OnFindButtonClicked);
		}
	}
}