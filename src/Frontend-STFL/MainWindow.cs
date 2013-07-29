/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2007, 2010-2013 Mirco Bauer <meebey@meebey.net>
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
using System.IO;
using System.Reflection;
using Mono.Unix;
using Smuxi.Common;
using Smuxi.Engine;
using Stfl;

namespace Smuxi.Frontend.Stfl
{
    public class MainWindow : Form
    {
#if LOG4NET
        static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        public StflUI UI { get; private set; }
        Entry Entry { get; set; }
        public ChatViewManager ChatViewManager { get; private set; }

        public string InputLabel {
            get {
                return this["input_label_text"];
            }
            set {
                this["input_label_text"] = String.Format("{0} ", value);
            }
        }

        public string NavigationLabel {
            get {
                return this["navigation_label_text"];
            }
            set {
                this["navigation_label_text"] = value;
            }
        }

        public bool ShowTitle {
            get {
                return this["title_hbox_display"] == "1";
            }
            set {
                this["title_hbox_display"] = value ? "1" : "0";
            }
        }

        public string TitleLabel {
            get {
                return this["title_label_text"];
            }
            set {
                this["title_label_text"] = value;
            }
        }

        public bool ShowTopic {
            get {
                return this["topic_hbox_display"] == "1";
            }
            set {
                this["topic_hbox_display"] = value ? "1" : "0";
            }
        }

        public string TopicLabel {
            get {
                return this["topic_label_text"];
            }
            set {
                this["topic_label_text"] = value;
            }
        }

        public MainWindow() : base(null, "MainWindow.stfl")
        {
            ChatViewManager = new ChatViewManager(this);
            Entry = new Entry(this, ChatViewManager);
            UI = new StflUI(ChatViewManager);

            Resized += OnResized;
            if (StflApi.IsXterm) {
                ShowTitle = false;
            }

            Assembly asm = Assembly.GetExecutingAssembly();
            ChatViewManager.Load(asm);
        }

        void OnResized(object sender, EventArgs e)
        {
#if LOG4NET
            Logger.DebugFormat(
                "OnResized(): terminal resized, columns: {0} lines: {1}",
                this["root_vbox:w"],
                this["root_vbox:h"]
            );
#endif
        }

        public void ApplyConfig(UserConfig config)
        {
            Entry.ApplyConfig(config);
        }
    }
}
