/**
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005 Mirco Bauer <meebey@meebey.net>
 *
 * Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
 */

using System;

namespace Meebey.Smuxi.FrontendGtkGnome
{
    public class CrashDialog : Gtk.Dialog
    {
        public CrashDialog(Exception e) : base()
        {
            SetDefaultSize(640, 480);
            Title = "smuxi - Oops, I did it again...";
            
            Gtk.HBox hbox = new Gtk.HBox();

            Gtk.Image image = new Gtk.Image(Gtk.Stock.DialogError, Gtk.IconSize.Dialog);
            hbox.PackStart(image, false, false, 2);

            Gtk.VBox label_vbox = new Gtk.VBox();
            Gtk.Label label1 = new Gtk.Label();
            Gtk.Label label2 = new Gtk.Label();
            label1.Markup = "<b>smuxi crashed because an unhandled exception was thrown!</b>";
            label2.Markup = "Here is the stacktrace, please report this bug!";
            label_vbox.PackStart(label1, false, false, 0);
            label_vbox.PackStart(new Gtk.Fixed(), true, true, 0);
            label_vbox.PackStart(label2, false, false, 0);
            hbox.PackStart(label_vbox, true, true, 0);
            
            Gtk.VBox vbox = new Gtk.VBox();
            vbox.PackStart(hbox, false, false, 2);
            
            Gtk.ScrolledWindow sw = new Gtk.ScrolledWindow();
            sw.ShadowType = Gtk.ShadowType.In;
            Gtk.TextView tv = new Gtk.TextView();
            tv.Editable = false;
            tv.CursorVisible = false;
            sw.Add(tv);
            vbox.PackStart(sw, true, true, 2);
            
            // add to the dialog
            VBox.PackStart(vbox, true, true, 2);
            AddButton(Gtk.Stock.Quit, 0);
            
            string message = "";
            if (e.InnerException != null) {
                message = "Inner-Exception Type:\n"+e.InnerException.GetType()+"\n\n"+
                          "Inner-Exception Message:\n"+e.InnerException.Message+"\n\n"+
                          "Inner-Exception StackTrace:\n"+e.InnerException.StackTrace+"\n";
            }
            message += "Exception Type:\n"+e.GetType()+"\n\n"+
                       "Exception Message:\n"+e.Message+"\n\n"+
                       "Exception StackTrace:\n"+e.StackTrace;
            tv.Buffer.Text = message;
            ShowAll();
            Run();
            Destroy();
       }
    }
}
