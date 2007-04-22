using System;
using System.Windows;
using Mono.Unix;

namespace Smuxi.Frontend.Wpf
{
    public static class NotImplementedMessageDialog
    {
        public static void Show()
        {
            MessageBox.Show(Catalog.GetString("Sorry, not implemented yet!"), null, MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
