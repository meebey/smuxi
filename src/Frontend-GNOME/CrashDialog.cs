/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2006, 2010 Mirco Bauer <meebey@meebey.net>
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
using Mono.Unix;
using Smuxi.Common;

namespace Smuxi.Frontend.Gnome
{
    public class CrashDialog : Gtk.Dialog
    {
#if LOG4NET
        private static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        string ReportSubject { get; set; }
        string ReportDescription { get; set; }

        public CrashDialog(Gtk.Window parent, Exception e) : base(null, parent, Gtk.DialogFlags.Modal)
        {
            SetDefaultSize(640, 480);
            Title = "Smuxi - " + _("Oops, I did it again...");

            Gtk.HBox hbox = new Gtk.HBox();

            Gtk.Image image = new Gtk.Image(Gtk.Stock.DialogError, Gtk.IconSize.Dialog);
            hbox.PackStart(image, false, false, 2);

            Gtk.VBox label_vbox = new Gtk.VBox();
            Gtk.Label label1 = new Gtk.Label();
            Gtk.Label label2 = new Gtk.Label();
            label1.Markup = String.Format(
                "<b>{0}</b>",
                GLib.Markup.EscapeText(
                    _("Smuxi crashed because an unhandled exception was thrown!")
                )
            );
            label2.Markup = GLib.Markup.EscapeText(
                _("Here is the stacktrace, please report this bug!")
            );
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
            AddButton(_("_Report Bug"), -1);
            AddButton(Gtk.Stock.Quit, 0);
            
            string message = String.Empty;
            if (e.InnerException != null) {
                message = "Inner-Exception Type:\n"+e.InnerException.GetType()+"\n\n"+
                          "Inner-Exception Message:\n"+e.InnerException.Message+"\n\n"+
                          "Inner-Exception StackTrace:\n"+e.InnerException.StackTrace+"\n\n";
                if (e.StackTrace != null &&
                    e.InnerException.StackTrace.Contains("System.Runtime.Remoting")) {
                    message += "Inner-Exception.ToString():\n"+e.InnerException.ToString()+"\n\n";
                }
            }
            message += "Exception Type:\n"+e.GetType()+"\n\n"+
                       "Exception Message:\n"+e.Message+"\n\n"+
                       "Exception StackTrace:\n"+e.StackTrace+"\n\n";
            if (e.StackTrace != null &&
                e.StackTrace.Contains("System.Runtime.Remoting")) {
                message += "Exception.ToString():\n"+e.ToString()+"\n\n";
            }
            tv.Buffer.Text = message;
            ReportSubject = "Exception: " + HtmlEncodeLame(e.Message);
            ReportDescription = String.Format(
                "<pre>{0}</pre>",
                HtmlEncodeLame("\n" + message)
            );
            
            ShowAll();
        }

        private string HtmlEncodeLame(string text)
        {
            if (text == null) {
                return String.Empty;
            }

            return text.Replace("&", "%26").
                        Replace(" ", "%20").
                        Replace("\n", "%0A").
                        Replace("<", "%3C").
                        Replace(">", "%3E");
        }
       
        public static void Show(Gtk.Window parent, Exception ex)
        {
            CrashDialog cd = new CrashDialog(parent, ex);
            cd.Run();
            cd.Destroy();
        }
        
        public new int Run()
        {
            int res;
            do {
                res = base.Run();
                if (res == -1) {
                    try {
                        System.Diagnostics.Process.Start(
                            String.Format(
                                "http://www.smuxi.org/issues/new" +
                                    "?issue[tracker_id]=1" +
                                    "&issue[subject]={0}" +
                                    "&issue[description]={1}",
                                ReportSubject,
                                ReportDescription
                            )
                        );
                    } catch (Exception ex) {
#if LOG4NET
                        f_Logger.Error(ex);
#endif
                    }
                }
            } while (res == -1);

            return res;
        }

        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
