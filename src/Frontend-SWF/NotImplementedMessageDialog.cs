using System;
using System.Windows.Forms;
using Mono.Unix;

namespace Smuxi.Frontend.Swf
{
	static class NotImplementedMessageDialog
	{
        static public void Show()
        {
            MessageBox.Show(Catalog.GetString("Sorry, not implemented yet!"), null, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
	}
}
