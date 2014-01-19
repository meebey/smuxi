// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2014 jamesaxl
//
// Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
using System;
using System.Collections;
using System.Collections.Generic;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    public class EmoticonsWindow : Gtk.Window
    {
        private Entry Entry{ get; set;}

        public EmoticonsWindow(Entry entry ,MainWindow mainWindow) : base(Gtk.WindowType.Popup)
        {
            var emoticonNoteBook = new Gtk.Notebook();
            EmoticonStore.ClearStore();
            var emoticonStore = EmoticonStore.InitStore();
            var emoticonsTableImage = new Gtk.Table(3,8,true);
            var emoticonsTableSymbol = new Gtk.Table(3,8,true);
            var emoticonButtonSymbolList = new List<EmoticonButton>();
            var emoticonButtonImageList = new List<EmoticonButton>();
            Entry = entry;
            foreach (var emoticon in emoticonStore) {
                emoticonButtonImageList.Add(new EmoticonButton(this, Entry, emoticon.Key as String, 
                                                          emoticon.Value as String));
                emoticonButtonSymbolList.Add(new EmoticonButton(this, Entry, emoticon.Key as String, 
                                                          emoticon.Value as String, false));
            }

            for (var i = 0; i <= 23; i++) {
                if (i < 8) {
                    emoticonsTableImage.Attach(emoticonButtonImageList [i], (uint) i, (uint) i + 1, 0, 1);
                    emoticonsTableSymbol.Attach(emoticonButtonSymbolList [i], (uint) i, (uint) i + 1, 0, 1);
                } else if (i >= 8 && i <= 15) {
                    var pos = (uint) i - 8;
                    emoticonsTableImage.Attach(emoticonButtonImageList [i], pos, pos + 1, 1, 2);
                    emoticonsTableSymbol.Attach(emoticonButtonSymbolList [i], pos, pos + 1, 1, 2);
                } else if (i >= 16 && i <= 23) {
                    var pos = (uint) i - 16;
                    emoticonsTableImage.Attach(emoticonButtonImageList[i], pos, pos + 1, 2, 3);
                    emoticonsTableSymbol.Attach(emoticonButtonSymbolList[i], pos, pos + 1, 2, 3);
                }
            }

            var imageTabLabel = new Gtk.Image();
            var symbolTabLabel = new Gtk.Label(":)");
            imageTabLabel.Pixbuf =  Frontend.LoadIcon("face-smile-symbolic", 32, "face-smile-symbolic.png");
            emoticonNoteBook.AppendPage(emoticonsTableImage, imageTabLabel);
            emoticonNoteBook.AppendPage(emoticonsTableSymbol, symbolTabLabel);
            emoticonNoteBook.SetTabLabelPacking(emoticonsTableImage, true, false, Gtk.PackType.Start);
            emoticonNoteBook.SetTabLabelPacking(emoticonsTableSymbol, true, false, Gtk.PackType.Start);
            Add(emoticonNoteBook);
            mainWindow.FocusOutEvent += DestroyEmoticonsWindow;
            mainWindow.FocusChildSet += DestroyEmoticonsWindow;
        }

        private void DestroyEmoticonsWindow(object obj, EventArgs args)
        {
            Destroy();
        }
    }
}
