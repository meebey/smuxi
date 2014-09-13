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
using Smuxi.SmuxiPasteBin;
using System.Collections.Specialized;

namespace Smuxi.Frontend.Gnome
{
    public class SmuxiPasteBinDialog : Gtk.Window
    {
        private NameValueCollection Values;
        private PoundPython _PoundPython;
        private PoeNoPaste _PoeNoPaste;
        private Pastebin _Pastebin;

        public SmuxiPasteBinDialog(string[] text, Gtk.TextView tv) : base ("PasteBin")
        {
            var vbox = new Gtk.VBox(false, 3);
            var hbox = new Gtk.HBox(false, 3);
            var cb = Gtk.ComboBox.NewText ();
            var bt = new Gtk.Button();
            cb.AppendText("PastBin");
            cb.AppendText("PoeNoPast");
            cb.AppendText("PoundPython");
            bt.Label = "SEND";
            var entryScrol = new Gtk.ScrolledWindow();
            var pasteBinEntry = new Gtk.TextView();
            entryScrol.ShadowType = Gtk.ShadowType.EtchedIn;
            var buffer = pasteBinEntry.Buffer;
            var insertIter = buffer.StartIter;
            foreach(var msg in text)
                buffer.Insert(ref insertIter, msg + '\n');
            SetPosition(Gtk.WindowPosition.CenterAlways);
            this.SetDefaultSize(800, 500);
            entryScrol.Add(pasteBinEntry);
            hbox.PackStart(cb, false, false, 7);
            hbox.PackStart(bt, false, false, 7);
            vbox.PackStart(hbox, false, false, 7);
            vbox.PackStart(entryScrol, true, true, 7);
            Add(vbox);
            bt.Clicked += delegate(object obj, EventArgs args) {
                Values = new NameValueCollection();
                if (cb.ActiveText == "PoundPython") {
                    Values["code"] = pasteBinEntry.Buffer.Text;
                    Values["language"] = "text";
                    Values["webpage"] = "";
                    _PoundPython = new PoundPython(Values);
                    tv.Buffer.Text = _PoundPython.ResponseString;
                } else if (cb.ActiveText == "PoeNoPast"){
                    Values["paste"] = pasteBinEntry.Buffer.Text;
                    _PoeNoPaste = new PoeNoPaste(Values);
                    tv.Buffer.Text = _PoeNoPaste.ResponseString;
                } else if (cb.ActiveText == "PastBin") {
                    Values["api_option"] = "paste";
                    Values["api_user_key"] = "" ;
                    Values["api_paste_private"] = "1";
                    Values["api_paste_name"] = "test";
                    Values["api_paste_expire_date"] = "10M";
                    Values["api_paste_format"] = "text";
                    Values["api_dev_key"] = "dc19a83fdd7e1dfe65e7263b141e9a6a";
                    Values["api_paste_code"] = pasteBinEntry.Buffer.Text;
                    _Pastebin = new Pastebin(Values);
                    tv.Buffer.Text = _Pastebin.ResponseString;
                }
                this.Destroy();
            };
        }
    }
}

