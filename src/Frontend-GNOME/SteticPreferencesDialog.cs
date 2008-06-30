namespace Smuxi.Frontend.Gnome
{
    public partial class SteticPreferencesDialog : Gtk.Dialog
    {
        public SteticPreferencesDialog()
        {
            Build();
        }
        
        protected virtual void _OnChanged(object sender, System.EventArgs e)
        {
        }
    }
}
