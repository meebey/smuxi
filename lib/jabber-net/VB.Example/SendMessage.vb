Public Class SendMessage
    Inherits System.Windows.Forms.Form

#Region " Windows Form Designer generated code "

    Private m_jc As jabber.client.JabberClient

    Public Sub New(ByRef jc As jabber.client.JabberClient)
        MyBase.New()

        m_jc = jc

        'This call is required by the Windows Form Designer.
        InitializeComponent()
    End Sub

    Public Sub New(ByRef jc As jabber.client.JabberClient, ByVal toJid As String)
        Me.New(jc)

        txtTo.Text = toJid
    End Sub


    'Form overrides dispose to clean up the component list.
    Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
        If disposing Then
            If Not (components Is Nothing) Then
                components.Dispose()
            End If
        End If
        MyBase.Dispose(disposing)
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    Friend WithEvents Panel1 As System.Windows.Forms.Panel
    Friend WithEvents btnCancel As System.Windows.Forms.Button
    Friend WithEvents btnSend As System.Windows.Forms.Button
    Friend WithEvents txtSubject As System.Windows.Forms.TextBox
    Friend WithEvents txtTo As System.Windows.Forms.TextBox
    Friend WithEvents label2 As System.Windows.Forms.Label
    Friend WithEvents label1 As System.Windows.Forms.Label
    Friend WithEvents txtBody As System.Windows.Forms.TextBox
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
        Me.Panel1 = New System.Windows.Forms.Panel()
        Me.btnCancel = New System.Windows.Forms.Button()
        Me.btnSend = New System.Windows.Forms.Button()
        Me.txtSubject = New System.Windows.Forms.TextBox()
        Me.txtTo = New System.Windows.Forms.TextBox()
        Me.label2 = New System.Windows.Forms.Label()
        Me.label1 = New System.Windows.Forms.Label()
        Me.txtBody = New System.Windows.Forms.TextBox()
        Me.Panel1.SuspendLayout()
        Me.SuspendLayout()
        '
        'Panel1
        '
        Me.Panel1.Controls.AddRange(New System.Windows.Forms.Control() {Me.btnCancel, Me.btnSend, Me.txtSubject, Me.txtTo, Me.label2, Me.label1})
        Me.Panel1.Dock = System.Windows.Forms.DockStyle.Top
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(312, 72)
        Me.Panel1.TabIndex = 0
        '
        'btnCancel
        '
        Me.btnCancel.Anchor = (System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right)
        Me.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.btnCancel.Location = New System.Drawing.Point(256, 41)
        Me.btnCancel.Name = "btnCancel"
        Me.btnCancel.Size = New System.Drawing.Size(48, 23)
        Me.btnCancel.TabIndex = 11
        Me.btnCancel.Text = "Cancel"
        '
        'btnSend
        '
        Me.btnSend.Anchor = (System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right)
        Me.btnSend.Location = New System.Drawing.Point(256, 9)
        Me.btnSend.Name = "btnSend"
        Me.btnSend.Size = New System.Drawing.Size(48, 23)
        Me.btnSend.TabIndex = 10
        Me.btnSend.Text = "Send"
        '
        'txtSubject
        '
        Me.txtSubject.Anchor = ((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right)
        Me.txtSubject.Location = New System.Drawing.Point(64, 42)
        Me.txtSubject.Name = "txtSubject"
        Me.txtSubject.Size = New System.Drawing.Size(184, 20)
        Me.txtSubject.TabIndex = 9
        Me.txtSubject.Text = ""
        '
        'txtTo
        '
        Me.txtTo.Anchor = ((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right)
        Me.txtTo.Location = New System.Drawing.Point(64, 10)
        Me.txtTo.Name = "txtTo"
        Me.txtTo.Size = New System.Drawing.Size(184, 20)
        Me.txtTo.TabIndex = 7
        Me.txtTo.Text = ""
        '
        'label2
        '
        Me.label2.Location = New System.Drawing.Point(8, 41)
        Me.label2.Name = "label2"
        Me.label2.Size = New System.Drawing.Size(48, 23)
        Me.label2.TabIndex = 8
        Me.label2.Text = "Subject:"
        '
        'label1
        '
        Me.label1.Location = New System.Drawing.Point(8, 9)
        Me.label1.Name = "label1"
        Me.label1.Size = New System.Drawing.Size(48, 23)
        Me.label1.TabIndex = 6
        Me.label1.Text = "To:"
        '
        'txtBody
        '
        Me.txtBody.Dock = System.Windows.Forms.DockStyle.Fill
        Me.txtBody.Location = New System.Drawing.Point(0, 72)
        Me.txtBody.Multiline = True
        Me.txtBody.Name = "txtBody"
        Me.txtBody.ScrollBars = System.Windows.Forms.ScrollBars.Both
        Me.txtBody.Size = New System.Drawing.Size(312, 194)
        Me.txtBody.TabIndex = 1
        Me.txtBody.Text = ""
        '
        'SendMessage
        '
        Me.AutoScaleBaseSize = New System.Drawing.Size(5, 13)
        Me.ClientSize = New System.Drawing.Size(312, 266)
        Me.Controls.AddRange(New System.Windows.Forms.Control() {Me.txtBody, Me.Panel1})
        Me.Name = "SendMessage"
        Me.Text = "SendMessage"
        Me.Panel1.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub

#End Region

    Private Sub btnSend_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSend.Click
        Dim msg As New jabber.protocol.client.Message(m_jc.Document)

        msg.To = New jabber.JID(txtTo.Text)
        If txtSubject.Text <> "" Then
            msg.Subject = txtSubject.Text
        End If
        msg.Body = txtBody.Text
        m_jc.Write(msg)
        Me.Close()
    End Sub

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        Me.Close()
    End Sub
End Class
