using Smuxi.Common;
using Smuxi.Engine;
using Smuxi.Frontend;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Collections;

namespace Smuxi.Frontend.Swf
{
    public partial class PreferencesDialog : Form
    {
        private delegate void Action();
        private event Action LoadConfig;
        private event Action SaveConfig;
        private UserConfig _UserConfig;

        public UserConfig UserConfig
        {
            get { return _UserConfig; }
            set { 
                _UserConfig = value;
                OnLoadConfig();
            }
        }

        public PreferencesDialog()
        {
            InitializeComponent();
            SelectionPanel.Items.AddRange((new ArrayList(MainPanel.Controls)).ToArray());
            SelectionPanel.SelectedValueChanged += delegate {
                Panel panel = SelectionPanel.SelectedItem as Panel;
                if (panel != null) {
                    panel.BringToFront();
                }
            };

            #region Nickname/Username/Realname

            LoadConfig += delegate {
                NicknameTextBox.Text = string.Join(" ", (string[])_UserConfig["Connection/Nicknames"]);
                UsernameTextBox.Text = (string)_UserConfig["Connection/Username"];
                RealnameTextBox.Text = (string)_UserConfig["Connection/Realname"];
            };

            SaveConfig += delegate {
                _UserConfig["Connection/Nicknames"] = NicknameTextBox.Text;
                _UserConfig["Connection/Username"] = UsernameTextBox.Text;
                _UserConfig["Connection/Realname"] = RealnameTextBox.Text;
            };

            #endregion
        }

        private void OnLoadConfig()
        {
            LoadConfig();
        }

        private void OnSaveConfig()
        {
            SaveConfig();
        }


        public static void Main()
        {
            PreferencesDialog pref = new PreferencesDialog();
            Config config = new Config();
            config.Load();
            UserConfig userConfig = new UserConfig(config, "local");
            pref.UserConfig = userConfig;
            Application.Run(pref);
        }

    }
}