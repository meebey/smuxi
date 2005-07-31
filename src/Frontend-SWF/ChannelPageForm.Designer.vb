<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Public Class ChannelPageForm
    Inherits PageForm

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
        Me.Userlist = New System.Windows.Forms.ListBox
        Me.Splitter1 = New System.Windows.Forms.Splitter
        Me.SuspendLayout()
        '
        'Userlist
        '
        Me.Userlist.DisplayMember = "NickName"
        Me.Userlist.Dock = System.Windows.Forms.DockStyle.Right
        Me.Userlist.FormattingEnabled = True
        Me.Userlist.IntegralHeight = False
        Me.Userlist.Location = New System.Drawing.Point(472, 0)
        Me.Userlist.Name = "Userlist"
        Me.Userlist.Size = New System.Drawing.Size(120, 350)
        Me.Userlist.TabIndex = 2
        '
        'Splitter1
        '
        Me.Splitter1.Dock = System.Windows.Forms.DockStyle.Right
        Me.Splitter1.Location = New System.Drawing.Point(469, 0)
        Me.Splitter1.Name = "Splitter1"
        Me.Splitter1.Size = New System.Drawing.Size(3, 350)
        Me.Splitter1.TabIndex = 5
        Me.Splitter1.TabStop = False
        '
        'ChannelPageForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(592, 373)
        Me.Controls.Add(Me.Splitter1)
        Me.Controls.Add(Me.Userlist)
        Me.Name = "ChannelPageForm"
        Me.Text = "ChannelPageForm"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Userlist As System.Windows.Forms.ListBox
    Friend WithEvents Splitter1 As System.Windows.Forms.Splitter
End Class
