// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2013 Mirco Bauer <meebey@meebey.net>
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
using System;

namespace Gtk.Extensions
{
    public static class ComboBoxExtensions
    {
        public static string GetActiveText(this Gtk.ComboBox comboBox)
        {
            Gtk.TreeIter activeIter;
            if (!comboBox.GetActiveIter(out activeIter)) {
                return null;
            }
#if GTK_SHARP_3
            var textColumn = comboBox.EntryTextColumn;
#else
            var textColumn = 0;
            if (comboBox is Gtk.ComboBoxEntry) {
                var entry = (Gtk.ComboBoxEntry) comboBox;
                textColumn = entry.TextColumn;
            }
#endif
            return (string) comboBox.Model.GetValue(activeIter, textColumn);
        }
    }

    public static class BoxExtensions
    {
        public static void PackStart(this Gtk.Box box, Gtk.Widget widget)
        {
            box.PackStart(widget, true, true, 0);
        }
    }
}
