'*
'* $Id$
'* $URL$
'* $Rev$
'* $Author$
'* $Date$
'*
'* smuxi - Smart MUltipleXed Irc
'*
'* Copyright (c) 2005 Jeffrey Richardson <themann@indyfantasysports.net>
'* Copyright (c) 2005 Smuxi Project <http://smuxi.meebey.net>
'*
'* Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
'*
'* This program is free software; you can redistribute it and/or modify
'* it under the terms of the GNU General Public License as published by
'* the Free Software Foundation; either version 2 of the License, or
'* (at your option) any later version.
'*
'* This program is distributed in the hope that it will be useful,
'* but WITHOUT ANY WARRANTY; without even the implied warranty of
'* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'* GNU General Public License for more details.
'*
'* You should have received a copy of the GNU General Public License
'* along with this program; if not, write to the Free Software
'* Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
'*

Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Public Class MainForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
        If disposing AndAlso components IsNot Nothing Then
            components.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim ToolStripSeparator1 As System.Windows.Forms.ToolStripSeparator
        Me.MainMenu = New System.Windows.Forms.MenuStrip
        Me.FileStrip = New System.Windows.Forms.ToolStripMenuItem
        Me.ConnectItem = New System.Windows.Forms.ToolStripMenuItem
        Me.ExitItem = New System.Windows.Forms.ToolStripMenuItem
        Me.WindowStrip = New System.Windows.Forms.ToolStripMenuItem
        Me.HelpStrip = New System.Windows.Forms.ToolStripMenuItem
        Me.AboutItem = New System.Windows.Forms.ToolStripMenuItem
        Me.MainToolBar = New System.Windows.Forms.ToolStrip
        Me.ToolStripLabel1 = New System.Windows.Forms.ToolStripLabel
        Me.NetworkBox = New System.Windows.Forms.ToolStripComboBox
        Me.StatusBar = New System.Windows.Forms.StatusStrip
        Me.CurrentNetwork = New System.Windows.Forms.ToolStripStatusLabel
        Me.Status = New System.Windows.Forms.ToolStripStatusLabel
        Me.PageList = New System.Windows.Forms.TreeView
        Me.ToolStripDockTop = New System.Windows.Forms.ToolStripPanel
        Me.ToolStripDockBottom = New System.Windows.Forms.ToolStripPanel
        Me.ToolStripDockLeft = New System.Windows.Forms.ToolStripPanel
        Me.ToolStripDockRight = New System.Windows.Forms.ToolStripPanel
        Me.Splitter1 = New System.Windows.Forms.Splitter
        ToolStripSeparator1 = New System.Windows.Forms.ToolStripSeparator
        Me.MainMenu.SuspendLayout()
        Me.MainToolBar.SuspendLayout()
        Me.StatusBar.SuspendLayout()
        Me.ToolStripDockTop.SuspendLayout()
        Me.SuspendLayout()
        '
        'ToolStripSeparator1
        '
        ToolStripSeparator1.Name = "ToolStripSeparator1"
        '
        'MainMenu
        '
        Me.MainMenu.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.FileStrip, Me.WindowStrip, Me.HelpStrip})
        Me.MainMenu.Location = New System.Drawing.Point(0, 0)
        Me.MainMenu.MdiWindowListItem = Me.WindowStrip
        Me.MainMenu.Name = "MainMenu"
        Me.MainMenu.Size = New System.Drawing.Size(792, 24)
        Me.MainMenu.TabIndex = 0
        Me.MainMenu.Text = "MenuStrip1"
        '
        'FileStrip
        '
        Me.FileStrip.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ConnectItem, ToolStripSeparator1, Me.ExitItem})
        Me.FileStrip.Name = "FileStrip"
        Me.FileStrip.Text = "&File"
        '
        'ConnectItem
        '
        Me.ConnectItem.Name = "ConnectItem"
        Me.ConnectItem.Text = "&Connect..."
        '
        'ExitItem
        '
        Me.ExitItem.Name = "ExitItem"
        Me.ExitItem.Text = "E&xit"
        '
        'WindowStrip
        '
        Me.WindowStrip.Name = "WindowStrip"
        Me.WindowStrip.Text = "&Window"
        '
        'HelpStrip
        '
        Me.HelpStrip.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.AboutItem})
        Me.HelpStrip.Name = "HelpStrip"
        Me.HelpStrip.Text = "&Help"
        '
        'AboutItem
        '
        Me.AboutItem.Name = "AboutItem"
        Me.AboutItem.Text = "&About..."
        '
        'MainToolBar
        '
        Me.MainToolBar.Dock = System.Windows.Forms.DockStyle.None
        Me.MainToolBar.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ToolStripLabel1, Me.NetworkBox})
        Me.MainToolBar.Location = New System.Drawing.Point(0, 0)
        Me.MainToolBar.Name = "MainToolBar"
        Me.MainToolBar.Size = New System.Drawing.Size(178, 25)
        Me.MainToolBar.TabIndex = 1
        Me.MainToolBar.Text = "ToolStrip1"
        '
        'ToolStripLabel1
        '
        Me.ToolStripLabel1.Name = "ToolStripLabel1"
        Me.ToolStripLabel1.Text = "Network:"
        '
        'NetworkBox
        '
        Me.NetworkBox.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText
        Me.NetworkBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.NetworkBox.Name = "NetworkBox"
        Me.NetworkBox.Size = New System.Drawing.Size(121, 25)
        '
        'StatusBar
        '
        Me.StatusBar.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.CurrentNetwork, Me.Status})
        Me.StatusBar.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.Table
        Me.StatusBar.Location = New System.Drawing.Point(0, 530)
        Me.StatusBar.Name = "StatusBar"
        Me.StatusBar.ShowItemToolTips = True
        Me.StatusBar.Size = New System.Drawing.Size(792, 23)
        Me.StatusBar.TabIndex = 0
        '
        'CurrentNetwork
        '
        Me.CurrentNetwork.AutoToolTip = True
        Me.CurrentNetwork.BorderSides = CType((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left Or System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) _
                    Or System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) _
                    Or System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom), System.Windows.Forms.ToolStripStatusLabelBorderSides)
        Me.CurrentNetwork.BorderStyle = System.Windows.Forms.Border3DStyle.SunkenOuter
        Me.CurrentNetwork.DoubleClickEnabled = True
        Me.CurrentNetwork.Name = "CurrentNetwork"
        Me.CurrentNetwork.Spring = True
        Me.CurrentNetwork.Text = "Current Network:"
        Me.CurrentNetwork.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'Status
        '
        Me.Status.AutoToolTip = True
        Me.Status.BorderSides = CType((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left Or System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) _
                    Or System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) _
                    Or System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom), System.Windows.Forms.ToolStripStatusLabelBorderSides)
        Me.Status.BorderStyle = System.Windows.Forms.Border3DStyle.SunkenOuter
        Me.Status.Name = "Status"
        Me.Status.Spring = True
        Me.Status.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'PageList
        '
        Me.PageList.BackColor = WinSmuxi.Settings.Default.BackgroundColor
        Me.PageList.DataBindings.Add(New System.Windows.Forms.Binding("BackColor", WinSmuxi.Settings.Default, "BackgroundColor", True, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged))
        Me.PageList.Dock = System.Windows.Forms.DockStyle.Left
        Me.PageList.Location = New System.Drawing.Point(0, 49)
        Me.PageList.Name = "PageList"
        Me.PageList.Size = New System.Drawing.Size(160, 481)
        Me.PageList.TabIndex = 0
        '
        'ToolStripDockTop
        '
        Me.ToolStripDockTop.Controls.Add(Me.MainToolBar)
        Me.ToolStripDockTop.Dock = System.Windows.Forms.DockStyle.Top
        Me.ToolStripDockTop.Location = New System.Drawing.Point(0, 24)
        Me.ToolStripDockTop.Name = "ToolStripDockTop"
        Me.ToolStripDockTop.Orientation = System.Windows.Forms.Orientation.Horizontal
        Me.ToolStripDockTop.RowMargin = New System.Windows.Forms.Padding(0)
        Me.ToolStripDockTop.Size = New System.Drawing.Size(792, 25)
        '
        'ToolStripDockBottom
        '
        Me.ToolStripDockBottom.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.ToolStripDockBottom.Location = New System.Drawing.Point(0, 530)
        Me.ToolStripDockBottom.Name = "ToolStripDockBottom"
        Me.ToolStripDockBottom.Orientation = System.Windows.Forms.Orientation.Horizontal
        Me.ToolStripDockBottom.RowMargin = New System.Windows.Forms.Padding(0)
        Me.ToolStripDockBottom.Size = New System.Drawing.Size(792, 0)
        '
        'ToolStripDockLeft
        '
        Me.ToolStripDockLeft.Dock = System.Windows.Forms.DockStyle.Left
        Me.ToolStripDockLeft.Location = New System.Drawing.Point(0, 49)
        Me.ToolStripDockLeft.Name = "ToolStripDockLeft"
        Me.ToolStripDockLeft.Orientation = System.Windows.Forms.Orientation.Vertical
        Me.ToolStripDockLeft.RowMargin = New System.Windows.Forms.Padding(0)
        Me.ToolStripDockLeft.Size = New System.Drawing.Size(0, 481)
        '
        'ToolStripDockRight
        '
        Me.ToolStripDockRight.Dock = System.Windows.Forms.DockStyle.Right
        Me.ToolStripDockRight.Location = New System.Drawing.Point(792, 49)
        Me.ToolStripDockRight.Name = "ToolStripDockRight"
        Me.ToolStripDockRight.Orientation = System.Windows.Forms.Orientation.Vertical
        Me.ToolStripDockRight.RowMargin = New System.Windows.Forms.Padding(0)
        Me.ToolStripDockRight.Size = New System.Drawing.Size(0, 481)
        '
        'Splitter1
        '
        Me.Splitter1.Location = New System.Drawing.Point(160, 49)
        Me.Splitter1.Name = "Splitter1"
        Me.Splitter1.Size = New System.Drawing.Size(3, 481)
        Me.Splitter1.TabIndex = 7
        Me.Splitter1.TabStop = False
        '
        'MainForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(792, 553)
        Me.Controls.Add(Me.Splitter1)
        Me.Controls.Add(Me.ToolStripDockRight)
        Me.Controls.Add(Me.PageList)
        Me.Controls.Add(Me.ToolStripDockLeft)
        Me.Controls.Add(Me.ToolStripDockBottom)
        Me.Controls.Add(Me.ToolStripDockTop)
        Me.Controls.Add(Me.StatusBar)
        Me.Controls.Add(Me.MainMenu)
        Me.IsMdiContainer = True
        Me.MainMenuStrip = Me.MainMenu
        Me.Name = "MainForm"
        Me.Text = "WinSmuxi"
        Me.MainMenu.ResumeLayout(False)
        Me.MainToolBar.ResumeLayout(False)
        Me.StatusBar.ResumeLayout(False)
        Me.ToolStripDockTop.ResumeLayout(False)
        Me.ToolStripDockTop.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents MainMenu As System.Windows.Forms.MenuStrip
    Friend WithEvents FileStrip As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ExitItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ConnectItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents HelpStrip As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents AboutItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents StatusBar As System.Windows.Forms.StatusStrip
    Friend WithEvents MainToolBar As System.Windows.Forms.ToolStrip
    Friend WithEvents PageList As System.Windows.Forms.TreeView
    Friend WithEvents ToolStripDockTop As System.Windows.Forms.ToolStripPanel
    Friend WithEvents ToolStripDockBottom As System.Windows.Forms.ToolStripPanel
    Friend WithEvents ToolStripDockLeft As System.Windows.Forms.ToolStripPanel
    Friend WithEvents ToolStripDockRight As System.Windows.Forms.ToolStripPanel
    Friend WithEvents Splitter1 As System.Windows.Forms.Splitter
    Friend WithEvents CurrentNetwork As System.Windows.Forms.ToolStripStatusLabel
    Friend WithEvents Status As System.Windows.Forms.ToolStripStatusLabel
    Friend WithEvents WindowStrip As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ToolStripLabel1 As System.Windows.Forms.ToolStripLabel
    Friend WithEvents NetworkBox As System.Windows.Forms.ToolStripComboBox

End Class
