using System;
using System.Windows;
using Mono.Unix;

namespace Smuxi.Frontend.Wpf
{
	static class NotImplementedMessageDialog
	{
        static public void Show()
        {
            MessageBox.Show(Catalog.GetString("Sorry, not implemented yet!"), null, MessageBoxButton.OK, MessageBoxImage.Information);
        }
	}
}
