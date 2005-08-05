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
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Public Class PageForm
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
        Me.CommandLine = New System.Windows.Forms.TextBox
        Me.PageBuffer = New System.Windows.Forms.RichTextBox
        Me.Send = New System.Windows.Forms.Button
        Me.TableLayoutPanel1 = New System.Windows.Forms.TableLayoutPanel
        Me.TableLayoutPanel1.SuspendLayout()
        Me.SuspendLayout()
        '
        'CommandLine
        '
        Me.CommandLine.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.CommandLine.Location = New System.Drawing.Point(0, 1)
        Me.CommandLine.Margin = New System.Windows.Forms.Padding(0)
        Me.CommandLine.Name = "CommandLine"
        Me.CommandLine.Size = New System.Drawing.Size(517, 20)
        Me.CommandLine.TabIndex = 0
        '
        'PageBuffer
        '
        Me.PageBuffer.BackColor = System.Drawing.SystemColors.Window
        Me.PageBuffer.Dock = System.Windows.Forms.DockStyle.Fill
        Me.PageBuffer.Font = New System.Drawing.Font("Courier New", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.PageBuffer.Location = New System.Drawing.Point(0, 0)
        Me.PageBuffer.Margin = New System.Windows.Forms.Padding(0)
        Me.PageBuffer.Name = "PageBuffer"
        Me.PageBuffer.ReadOnly = True
        Me.PageBuffer.RichTextShortcutsEnabled = False
        Me.PageBuffer.ShortcutsEnabled = False
        Me.PageBuffer.ShowSelectionMargin = True
        Me.PageBuffer.Size = New System.Drawing.Size(592, 350)
        Me.PageBuffer.TabIndex = 1
        Me.PageBuffer.Text = ""
        '
        'Send
        '
        Me.Send.Anchor = System.Windows.Forms.AnchorStyles.None
        Me.Send.Location = New System.Drawing.Point(517, 0)
        Me.Send.Margin = New System.Windows.Forms.Padding(0)
        Me.Send.Name = "Send"
        Me.Send.Size = New System.Drawing.Size(75, 23)
        Me.Send.TabIndex = 1
        Me.Send.Text = "Send"
        '
        'TableLayoutPanel1
        '
        Me.TableLayoutPanel1.AutoSize = True
        Me.TableLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
        Me.TableLayoutPanel1.ColumnCount = 2
        Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
        Me.TableLayoutPanel1.Controls.Add(Me.Send, 1, 0)
        Me.TableLayoutPanel1.Controls.Add(Me.CommandLine, 0, 0)
        Me.TableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.TableLayoutPanel1.Location = New System.Drawing.Point(0, 350)
        Me.TableLayoutPanel1.Name = "TableLayoutPanel1"
        Me.TableLayoutPanel1.RowCount = 1
        Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
        Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
        Me.TableLayoutPanel1.Size = New System.Drawing.Size(592, 23)
        Me.TableLayoutPanel1.TabIndex = 0
        '
        'PageForm
        '
        Me.AcceptButton = Me.Send
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(592, 373)
        Me.Controls.Add(Me.PageBuffer)
        Me.Controls.Add(Me.TableLayoutPanel1)
        Me.Name = "PageForm"
        Me.Text = "PageForm"
        Me.TableLayoutPanel1.ResumeLayout(False)
        Me.TableLayoutPanel1.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents CommandLine As System.Windows.Forms.TextBox
    Friend WithEvents PageBuffer As System.Windows.Forms.RichTextBox
    Friend WithEvents Send As System.Windows.Forms.Button
    Friend WithEvents TableLayoutPanel1 As System.Windows.Forms.TableLayoutPanel
End Class
