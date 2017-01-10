// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2013 Mirco Bauer <meebey@meebey.net>
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
using System.IO;
using System.Threading;
using System.Collections.Generic;
using Smuxi.Common;
using Smuxi.Engine;
using Process = System.Diagnostics.Process;
using UI = Gtk.Builder.ObjectAttribute;

namespace Smuxi.Frontend.Gnome
{
    public class PreferencesDialog : Gtk.Dialog
    {
        [UI("CategoryNotebook")] Gtk.Notebook f_CategoryNotebook;
        [UI("ConnectionToggleButton")] Gtk.ToggleButton f_ConnectionToggleButton;
        [UI("InterfaceToggleButton")] Gtk.ToggleButton f_InterfaceToggleButton;
        [UI("ServersToggleButton")] Gtk.ToggleButton f_ServersToggleButton;
        [UI("FiltersToggleButton")] Gtk.ToggleButton f_FiltersToggleButton;
        [UI("LoggingToggleButton")] Gtk.ToggleButton f_LoggingToggleButton;
        [UI("FilterListBox")] Gtk.Box f_FilterListBox;
        [UI("ServerListBox")] Gtk.Box f_ServerListBox;
        [UI("SystemWideFontRadioButton")] Gtk.RadioButton f_SystemWideFontRadioButton;
        [UI("CustomFontRadioButton")] Gtk.RadioButton f_CustomFontRadioButton;
        [UI("FontButton")] Gtk.FontButton f_FontButton;
        [UI("SystemWideFontColorRadioButton")] Gtk.RadioButton f_SystemWideFontColorRadioButton;
        [UI("CustomFontColorRadioButton")] Gtk.RadioButton f_CustomFontColorRadioButton;
        [UI("ForegroundColorButton")] Gtk.ColorButton f_ForegroundColorButton;
        [UI("BackgroundColorButton")] Gtk.ColorButton f_BackgroundColorButton;
        [UI("ProxySwitch")] Gtk.CheckButton f_ProxySwitch;
        [UI("ProxyTypeComboBox")] Gtk.ComboBox f_ProxyTypeComboBox;
        [UI("ProxyHostEntry")] Gtk.Entry f_ProxyHostEntry;
        [UI("ProxyPortSpinButton")] Gtk.SpinButton f_ProxyPortSpinButton;
        [UI("ProxyUsernameEntry")] Gtk.Entry f_ProxyUsernameEntry;
        [UI("ProxyPasswordEntry")] Gtk.Entry f_ProxyPasswordEntry;
        [UI("ProxyShowPasswordCheckButton")] Gtk.CheckButton f_ProxyShowPasswordCheckButton;
        [UI("LoggingSwitch")] Gtk.CheckButton f_LoggingSwitch;
        [UI("LoggingOpenButton")] Gtk.Button f_LoggingOpenButton;
        [UI("LoggingLogFilteredMessagesCheckButton")] Gtk.CheckButton f_LoggingLogFilteredMessagesCheckButton;
        [UI("ShowColorsCheckButton")] Gtk.CheckButton f_ShowColorsCheckButton;
        [UI("ShowFormattingsCheckButton")] Gtk.CheckButton f_ShowFormattingsCheckButton;
        [UI("InternalSettingsToolbar")] Gtk.Toolbar f_InternalSettingsToolbar;
        Dictionary<string, string> ConfigKeyToWidgetNameMap { get; set; }
        new Gtk.Window Parent { get; set; }
        Gtk.Builder Builder { get; set; }
        FilterListWidget FilterListWidget { get; set; }
        ServerListView ServerListView { get; set; }

        public Category CurrentCategory {
            set {
                f_CategoryNotebook.Page = (int) value;
                switch (value) {
                    case Category.Connection:
                        if (!f_ConnectionToggleButton.Active) {
                            f_ConnectionToggleButton.Active = true;
                        }
                        f_InterfaceToggleButton.Active = false;
                        f_ServersToggleButton.Active = false;
                        f_FiltersToggleButton.Active = false;
                        f_LoggingToggleButton.Active = false;
                        break;
                    case Category.Interface:
                        if (!f_InterfaceToggleButton.Active) {
                            f_InterfaceToggleButton.Active = true;
                        }
                        f_ConnectionToggleButton.Active = false;
                        f_ServersToggleButton.Active = false;
                        f_FiltersToggleButton.Active = false;
                        f_LoggingToggleButton.Active = false;
                        break;
                    case Category.Servers:
                        if (!f_ServersToggleButton.Active) {
                            f_ServersToggleButton.Active = true;
                        }
                        f_ConnectionToggleButton.Active = false;
                        f_InterfaceToggleButton.Active = false;
                        f_FiltersToggleButton.Active = false;
                        f_LoggingToggleButton.Active = false;
                        break;
                    case Category.Filters:
                        if (!f_FiltersToggleButton.Active) {
                            f_FiltersToggleButton.Active = true;
                        }
                        f_ConnectionToggleButton.Active = false;
                        f_InterfaceToggleButton.Active = false;
                        f_ServersToggleButton.Active = false;
                        f_LoggingToggleButton.Active = false;
                        break;
                    case Category.Logging:
                        if (!f_LoggingToggleButton.Active) {
                            f_LoggingToggleButton.Active = true;
                        }
                        f_ConnectionToggleButton.Active = false;
                        f_InterfaceToggleButton.Active = false;
                        f_ServersToggleButton.Active = false;
                        f_FiltersToggleButton.Active = false;
                        break;
                }
            }
        }

        public PreferencesDialog(Gtk.Window parent, Gtk.Builder builder, IntPtr handle) :
                            base(handle)
        {
            Trace.Call(parent, builder, handle);

            if (parent == null) {
                throw new ArgumentNullException("parent");
            }
            if (builder == null) {
                throw new ArgumentNullException("builder");
            }
            if (handle == IntPtr.Zero) {
                throw new ArgumentException("handle", "handle must not be zero.");
            }

            Parent = parent;
            Builder = builder;
            Builder.Autoconnect(this);
            f_CategoryNotebook.ShowTabs = false;
            f_ConnectionToggleButton.Active = true;
            // not implemented
            f_InternalSettingsToolbar.NoShowAll = true;
            f_InternalSettingsToolbar.Visible = false;

            // Filters
            FilterListWidget = new FilterListWidget(parent, Frontend.UserConfig);
            FilterListWidget.InitProtocols(Frontend.Session.GetSupportedProtocols());
            FilterListWidget.Load();
            f_FilterListBox.Add(FilterListWidget);

            // Servers
            ServerListView = new ServerListView(parent);
            ServerListView.Load();
            f_ServerListBox.Add(ServerListView);

            Init();
            ReadFromConfig();

            ShowAll();
        }

        void Init()
        {
            ConfigKeyToWidgetNameMap = new Dictionary<string, string>();
            var map = ConfigKeyToWidgetNameMap;

            // Connection
            map.Add("Connection/Nicknames", "ConnectionNicknamesEntry");
            map.Add("Connection/Realname", "ConnectionRealnameEntry");
            map.Add("Connection/ProxyHostname", "ProxyHostEntry");
            map.Add("Connection/ProxyPort", "ProxyPortSpinButton");
            map.Add("Connection/ProxyUsername", "ProxyUsernameEntry");
            map.Add("Connection/ProxyPassword", "ProxyPasswordEntry");

            // Interface/Messages
            map.Add("Interface/Notebook/TimestampFormat", "TimestampFormatEntry");
            map.Add("Interface/Notebook/BufferLines", "BufferLinesSpinButton");
            // "Interface/Notebook/StripColors"
            // "Interface/Notebook/StripFormattings"

            // Interface/Tabs
            // "Interface/Notebook/TabPosition"
            map.Add("Interface/Notebook/AutoSwitchPersonChats", "AutoSwitchPersonChatsCheckButton");
            map.Add("Interface/Notebook/AutoSwitchGroupChats", "AutoSwitchGroupChatsCheckButton");

            // Interface/Notifications
            map.Add("Interface/Notebook/Tab/NoActivityColor", "NoActivityColorButton");
            map.Add("Interface/Notebook/Tab/ActivityColor", "ActivityColorButton");
            map.Add("Interface/Notebook/Tab/EventColor", "ModeColorButton");
            map.Add("Interface/Notebook/Tab/HighlightColor", "HighlightColorButton");

            // Interface/Input
            map.Add("Interface/Entry/CompletionCharacter", "CompletionCharacterEntry");
            map.Add("Interface/Entry/CommandCharacter", "CommandCharacterEntry");
            map.Add("Interface/Entry/CommandHistorySize", "CommandHistorySizeSpinButton");
            map.Add("Interface/Entry/BashStyleCompletion", "BashStyleCompletionSwitch");

            // Interface/Appearance
            map.Add("Interface/Notebook/Channel/NickColors", "ColoredNicknamesSwitch");
            // "Interface/Chat/FontFamily"
            // "Interface/Chat/FontStyle"
            // "Interface/Chat/FontSize"
            // "Interface/Chat/BackgroundColor"
            // "Interface/Chat/ForegroundColor"
            map.Add("Interface/Chat/ForegroundColor", "ForegroundColorButton");
            map.Add("Interface/Chat/BackgroundColor", "BackgroundColorButton");
            map.Add("Interface/Chat/HighlightWords", "HighlightWordsTextView");
            map.Add("Sound/BeepOnHighlight", "BeepOnHighlightCheckButton");

            // Logging
            map.Add("Logging/Enabled", "LoggingSwitch");
            map.Add("Logging/LogFilteredMessages", "LoggingLogFilteredMessagesCheckButton");

            // init widgets
            f_ProxyTypeComboBox.Clear();
            var cell = new Gtk.CellRendererText();
            f_ProxyTypeComboBox.PackStart(cell, false);
            f_ProxyTypeComboBox.AddAttribute(cell, "text", 1);
            var store = new Gtk.ListStore(typeof(ProxyType), typeof(string));
            // fill ListStore
            store.AppendValues(ProxyType.None,    String.Format("<{0}>",
                                                                _("No Proxy")));
            store.AppendValues(ProxyType.System,  String.Format("<{0}>",
                                                                _("System Default")));
            store.AppendValues(ProxyType.Http,    "HTTP");
            store.AppendValues(ProxyType.Socks4,  "SOCK 4");
            store.AppendValues(ProxyType.Socks4a, "SOCK 4a");
            store.AppendValues(ProxyType.Socks5,  "SOCK 5");
            f_ProxyTypeComboBox.Model = store;
            f_ProxyTypeComboBox.Active = 0;

            // font radio buttons
            f_SystemWideFontRadioButton.Toggled += (sender, e) => {
                CheckFontRadioButtons();
            };
            f_CustomFontRadioButton.Toggled += (sender, e) => {
                CheckFontRadioButtons();
            };

            // font color radio buttons
            f_SystemWideFontColorRadioButton.Toggled += (sender, e) => {
                CheckFontColorRadioButtons();
            };
            f_CustomFontColorRadioButton.Toggled += (sender, e) => {
                CheckFontColorRadioButtons();
            };

            f_ProxySwitch.AddNotification("active", delegate {
                CheckProxySwitch();
            });
            f_LoggingSwitch.AddNotification("active", delegate {
                CheckLoggingSwitch();
            });
        }

        void ReadFromConfig()
        {
            Trace.Call();

            var conf = Frontend.UserConfig;

            // manually handled widgets
            ProxyType proxyType = (ProxyType) Enum.Parse(
                typeof(ProxyType),
                (string) conf["Connection/ProxyType"]
            );
            int i = 0;
            foreach (object[] row in  (Gtk.ListStore) f_ProxyTypeComboBox.Model) {
                if (((ProxyType) row[0]) == proxyType) {
                    f_ProxyTypeComboBox.Active = i;
                    break;
                }
                i++;
            }
            f_ProxySwitch.Active = proxyType != ProxyType.None;
            CheckProxySwitch();

            f_ShowColorsCheckButton.Active = !(bool) conf["Interface/Notebook/StripColors"];
            f_ShowFormattingsCheckButton.Active = !(bool) conf["Interface/Notebook/StripFormattings"];

            var fontButton = (Gtk.FontButton) Builder.GetObject("FontButton");
            var fontFamily = (string) conf["Interface/Chat/FontFamily"];
            var fontStyle = (string) conf["Interface/Chat/FontStyle"];
            int fontSize = 0;
            if (conf["Interface/Chat/FontSize"] != null) {
                fontSize = (int) conf["Interface/Chat/FontSize"];
            }
            if (String.IsNullOrEmpty(fontFamily) &&
                String.IsNullOrEmpty(fontStyle) &&
                fontSize == 0) {
                f_SystemWideFontRadioButton.Active = true;
            } else {
                f_CustomFontRadioButton.Active = true;
                Pango.FontDescription fontDescription = new Pango.FontDescription();
                fontDescription.Family = fontFamily;
                string frontWeigth = null;
                if (fontStyle.Contains(" ")) {
                    int pos = fontStyle.IndexOf(" ");
                    frontWeigth = fontStyle.Substring(0, pos);
                    fontStyle = fontStyle.Substring(pos + 1);
                }
                fontDescription.Style = (Pango.Style) Enum.Parse(typeof(Pango.Style), fontStyle);
                if (frontWeigth != null) {
                    fontDescription.Weight = (Pango.Weight) Enum.Parse(typeof(Pango.Weight), frontWeigth);
                }
                fontDescription.Size = fontSize * 1024;
                fontButton.FontName = fontDescription.ToString();
            }
            var bgColorHexCode = (string) conf["Interface/Chat/BackgroundColor"];
            var fgColorHexCode = (string) conf["Interface/Chat/ForegroundColor"];
            if (String.IsNullOrEmpty(bgColorHexCode) &&
                String.IsNullOrEmpty(fgColorHexCode)) {
                f_SystemWideFontRadioButton.Active = true;
            } else {
                f_CustomFontColorRadioButton.Active = true;
            }

            // mapped widgets
            foreach (var confEntry in ConfigKeyToWidgetNameMap) {
                var confKey = confEntry.Key;
                var confValue = conf[confKey];
                var widgetId = confEntry.Value;
                var widget = Builder.GetObject(widgetId);
                if (widget is Gtk.SpinButton) {
                    var spinButton = (Gtk.SpinButton) widget;
                    if (confValue is Int32) {
                        spinButton.Value = (Int32) confValue;
                    } else {
                        spinButton.Value = Int32.Parse((string) confValue);
                    }
                } else if (widget is Gtk.ColorButton) {
                    var colorButton = (Gtk.ColorButton) widget;
                    var colorHexCode = (string) confValue;
                    if (String.IsNullOrEmpty(colorHexCode)) {
                        colorButton.Color = Gdk.Color.Zero;
                    } else {
                        colorButton.Color = ColorConverter.GetGdkColor(colorHexCode);
                    }
                } else if (widget is Gtk.CheckButton) {
                    var checkButton = (Gtk.CheckButton) widget;
                    checkButton.Active = (bool) confValue;
#if GTK_SHARP_3
                } else if (widget is Gtk.Switch) {
                    var @switch = (Gtk.Switch) widget;
                    @switch.Active = (bool) confValue;
#endif
                } else if (widget is Gtk.TextView) {
                    var textView = (Gtk.TextView) widget;
                    if (confValue is string[]) {
                        textView.Buffer.Text = String.Join("\n", (string[]) confValue);
                    } else {
                        textView.Buffer.Text = (string) confValue;
                    }
                } else if (widget is Gtk.Entry) {
                    var entry = (Gtk.Entry) widget;
                    if (confValue is string[]) {
                        entry.Text = String.Join(" ", (string[]) confValue);
                    } else {
                        entry.Text = (string) confValue;
                    }
                }
            }
        }

        void WriteToConfig()
        {
            Trace.Call();

            var conf = Frontend.UserConfig;

            // manually handled widgets
            if (f_ProxySwitch.Active) {
                Gtk.TreeIter iter;
                f_ProxyTypeComboBox.GetActiveIter(out iter);
                var proxyType = (ProxyType) f_ProxyTypeComboBox.Model.GetValue(iter, 0);
                conf["Connection/ProxyType"] = proxyType.ToString();
            } else {
                conf["Connection/ProxyType"] = ProxyType.None.ToString();
            }

            conf["Interface/Notebook/StripColors"] = !f_ShowColorsCheckButton.Active;
            conf["Interface/Notebook/StripFormattings"] = !f_ShowFormattingsCheckButton.Active;

            if (f_CustomFontRadioButton.Active) {
                string fontName = f_FontButton.FontName;
                Pango.FontDescription fontDescription = Pango.FontDescription.FromString(fontName);
                conf["Interface/Chat/FontFamily"] = fontDescription.Family;
                conf["Interface/Chat/FontStyle"] = fontDescription.Weight + " " + fontDescription.Style;
                conf["Interface/Chat/FontSize"] = fontDescription.Size / 1024;
            } else {
                conf["Interface/Chat/FontFamily"] = String.Empty;
                conf["Interface/Chat/FontStyle"] = String.Empty;
                conf["Interface/Chat/FontSize"] = 0;
            }

            // mapped widgets
            foreach (var confEntry in ConfigKeyToWidgetNameMap) {
                var confKey = confEntry.Key;
                var confValue = conf[confKey];
                var widgetId = confEntry.Value;
                var widget = Builder.GetObject(widgetId);
                if (widget is Gtk.SpinButton) {
                    var spinButton = (Gtk.SpinButton) widget;
                    if (confValue is Int32) {
                        conf[confKey] = spinButton.ValueAsInt;
                    }
                } else if (widget is Gtk.ColorButton) {
                    var colorButton = (Gtk.ColorButton) widget;
                    if (confValue is string) {
                        conf[confKey] = ColorConverter.GetHexCode(colorButton.Color);
                    }
                } else if (widget is Gtk.CheckButton) {
                    var checkButton = (Gtk.CheckButton) widget;
                    if (confValue is bool) {
                        conf[confKey] = checkButton.Active;
                    }
#if GTK_SHARP_3
                } else if (widget is Gtk.Switch) {
                    var @switch = (Gtk.Switch) widget;
                    if (confValue is bool) {
                        conf[confKey] = @switch.Active;
                    }
#endif
                } else if (widget is Gtk.TextView) {
                    var textView = (Gtk.TextView) widget;
                    if (confValue is string[]) {
                        conf[confKey] = textView.Buffer.Text.Split('\n');
                    } else {
                        conf[confKey] = textView.Buffer.Text;
                    }
                } else if (widget is Gtk.Entry) {
                    var entry = (Gtk.Entry) widget;
                    if (confValue is string[]) {
                        conf[confKey] = entry.Text.Split('\n');
                    } else {
                        conf[confKey] = entry.Text;
                    }
                }
            }

            // reset colors as there is no distinct key if they are custom or not
            if (f_SystemWideFontColorRadioButton.Active) {
                conf["Interface/Chat/ForegroundColor"] = String.Empty;
                conf["Interface/Chat/BackgroundColor"] = String.Empty;
            }

            conf.Save();
        }

        protected virtual void OnResponse(object sender, Gtk.ResponseArgs e)
        {
            Trace.Call(sender, e);

            WriteToConfig();
            Frontend.ApplyConfig(Frontend.UserConfig);
            Destroy();
        }

        protected virtual void OnConnectionToggleButtonToggled(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            if (f_ConnectionToggleButton.Active) {
                CurrentCategory = Category.Connection;
            }
        }

        protected virtual void OnInterfaceToggleButtonToggled(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            if (f_InterfaceToggleButton.Active) {
                CurrentCategory = Category.Interface;
            }
        }

        protected virtual void OnServersToggleButtonToggled(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            if (f_ServersToggleButton.Active) {
                CurrentCategory = Category.Servers;
            }
        }

        protected virtual void OnFiltersToggleButtonToggled(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            if (f_FiltersToggleButton.Active) {
                CurrentCategory = Category.Filters;
            }
        }

        protected virtual void OnLoggingToggleButtonToggled(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            if (f_LoggingToggleButton.Active) {
                CurrentCategory = Category.Logging;
            }
        }

        protected virtual void OnProxyShowPasswordCheckButtonToggled(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            f_ProxyPasswordEntry.Visibility = f_ProxyShowPasswordCheckButton.Active;
        }

        protected virtual void OnLoggingOpenButtonClicked(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    var logPath = Platform.LogPath;
                    if (!Directory.Exists(logPath)) {
                        Directory.CreateDirectory(logPath);
                    }
                    Process.Start(logPath);
                } catch (Exception ex) {
                    Frontend.ShowError(Parent, ex);
                }
            });
        }

        void CheckProxySwitch()
        {
            var isActive = f_ProxySwitch.Active;
            f_ProxyTypeComboBox.Sensitive = isActive;
            f_ProxyHostEntry.Sensitive = isActive;
            f_ProxyPortSpinButton.Sensitive = isActive;
            f_ProxyUsernameEntry.Sensitive = isActive;
            f_ProxyPasswordEntry.Sensitive = isActive;
            f_ProxyShowPasswordCheckButton.Sensitive = isActive;
        }

        void CheckLoggingSwitch()
        {
            var isActive = f_LoggingSwitch.Active;
            f_LoggingLogFilteredMessagesCheckButton.Sensitive = isActive;
            f_LoggingOpenButton.Sensitive = isActive;
        }

        void CheckFontRadioButtons()
        {
            f_FontButton.Sensitive = f_CustomFontRadioButton.Active;
        }

        void CheckFontColorRadioButtons()
        {
            var isCustomActive = f_CustomFontColorRadioButton.Active;
            f_ForegroundColorButton.Sensitive = isCustomActive;
            f_BackgroundColorButton.Sensitive = isCustomActive;
        }

        static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }

        public enum Category {
            Connection,
            Interface,
            Servers,
            Filters,
            Logging,
        }

        public enum InterfacePage {
            Messages,
            Tabs,
            Notifications,
            Input,
            Appearance,
        }
    }
}
