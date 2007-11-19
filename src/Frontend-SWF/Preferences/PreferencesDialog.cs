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
        public PreferencesDialog()
        {
            InitializeComponent();
            SelectionPanel.Items.AddRange((new ArrayList(MainPanel.Controls)).ToArray());
        }
        public static void Main(){
            Application.Run(new PreferencesDialog());
        }

    }
}