' --------------------------------------------------------------------------
'
' License
'
' The contents of this file are subject to the Jabber Open Source License
' Version 1.0 (the "License").  You may not copy or use this file, in either
' source code or executable form, except in compliance with the License.  You
' may obtain a copy of the License at http://www.jabber.com/license/ or at
' http://www.opensource.org/.  
'
' Software distributed under the License is distributed on an "AS IS" basis,
' WITHOUT WARRANTY OF ANY KIND, either express or implied.  See the License
' for the specific language governing rights and limitations under the
' License.
'
' Copyrights
' 
' Portions created by or assigned to Cursive Systems, Inc. are 
' Copyright (c) 2002-2004 Cursive Systems, Inc.  All Rights Reserved.  Contact
' information for Cursive Systems, Inc. is available at http://www.cursive.net/.
'
' Portions Copyright (c) 2002-2004 Joe Hildebrand.
' 
' Acknowledgements
' 
' Special thanks to the Jabber Open Source Contributors for their
' suggestions and support of Jabber.
' 
' --------------------------------------------------------------------------*/
Imports System.Diagnostics
Imports System.Xml

Imports jabber
Imports jabber.protocol
Imports jabber.protocol.client
Imports jabber.protocol.iq

Public Class MainForm
    Inherits System.Windows.Forms.Form

#Region " Windows Form Designer generated code "

    Public Sub New()
        MyBase.New()

        'This call is required by the Windows Form Designer.
        InitializeComponent()

        'Add any initialization after the InitializeComponent() call

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

    Private m_err As Boolean = False

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    Friend WithEvents sb As System.Windows.Forms.StatusBar
    Friend WithEvents pnlCon As System.Windows.Forms.StatusBarPanel
    Friend WithEvents pnlPresence As System.Windows.Forms.StatusBarPanel
    Friend WithEvents jc As jabber.client.JabberClient
    Friend WithEvents rm As jabber.client.RosterManager
    Friend WithEvents pm As jabber.client.PresenceManager
    Friend WithEvents ilPresence As System.Windows.Forms.ImageList
    Friend WithEvents mnuPresence As System.Windows.Forms.ContextMenu
    Friend WithEvents MenuItem3 As System.Windows.Forms.MenuItem
    Friend WithEvents TabControl1 As System.Windows.Forms.TabControl
    Friend WithEvents tpRoster As System.Windows.Forms.TabPage
    Friend WithEvents tpDebug As System.Windows.Forms.TabPage

    Friend WithEvents mnuAvailable As System.Windows.Forms.MenuItem
    Friend WithEvents mnuAway As System.Windows.Forms.MenuItem
    Friend WithEvents mnuOffline As System.Windows.Forms.MenuItem
    Friend WithEvents roster As muzzle.RosterTree
    Friend WithEvents debug As muzzle.BottomScrollRichText

    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container
        Dim resources As System.Resources.ResourceManager = New System.Resources.ResourceManager(GetType(MainForm))
        Me.sb = New System.Windows.Forms.StatusBar
        Me.pnlCon = New System.Windows.Forms.StatusBarPanel
        Me.pnlPresence = New System.Windows.Forms.StatusBarPanel
        Me.jc = New jabber.client.JabberClient(Me.components)
        Me.rm = New jabber.client.RosterManager(Me.components)
        Me.pm = New jabber.client.PresenceManager(Me.components)
        Me.ilPresence = New System.Windows.Forms.ImageList(Me.components)
        Me.mnuPresence = New System.Windows.Forms.ContextMenu
        Me.mnuAvailable = New System.Windows.Forms.MenuItem
        Me.mnuAway = New System.Windows.Forms.MenuItem
        Me.MenuItem3 = New System.Windows.Forms.MenuItem
        Me.mnuOffline = New System.Windows.Forms.MenuItem
        Me.TabControl1 = New System.Windows.Forms.TabControl
        Me.tpRoster = New System.Windows.Forms.TabPage
        Me.roster = New muzzle.RosterTree
        Me.tpDebug = New System.Windows.Forms.TabPage
        Me.debug = New muzzle.BottomScrollRichText
        CType(Me.pnlCon, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.pnlPresence, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.TabControl1.SuspendLayout()
        Me.tpRoster.SuspendLayout()
        Me.tpDebug.SuspendLayout()
        Me.SuspendLayout()
        '
        'sb
        '
        Me.sb.Location = New System.Drawing.Point(0, 244)
        Me.sb.Name = "sb"
        Me.sb.Panels.AddRange(New System.Windows.Forms.StatusBarPanel() {Me.pnlCon, Me.pnlPresence})
        Me.sb.ShowPanels = True
        Me.sb.Size = New System.Drawing.Size(632, 22)
        Me.sb.TabIndex = 0
        '
        'pnlCon
        '
        Me.pnlCon.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Spring
        Me.pnlCon.Text = "Click on ""Offline"", and select a presence to log in."
        Me.pnlCon.Width = 569
        '
        'pnlPresence
        '
        Me.pnlPresence.Alignment = System.Windows.Forms.HorizontalAlignment.Right
        Me.pnlPresence.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Contents
        Me.pnlPresence.Text = "Offline"
        Me.pnlPresence.Width = 47
        '
        'jc
        '
        Me.jc.AutoReconnect = 3.0!
        Me.jc.AutoStartTLS = True
        Me.jc.InvokeControl = Me
        Me.jc.LocalCertificate = Nothing
        Me.jc.Password = Nothing
        Me.jc.User = Nothing
        '
        'rm
        '
        Me.rm.Client = Me.jc
        '
        'pm
        '
        Me.pm.Client = Me.jc
        '
        'ilPresence
        '
        Me.ilPresence.ImageSize = New System.Drawing.Size(20, 20)
        Me.ilPresence.ImageStream = CType(resources.GetObject("ilPresence.ImageStream"), System.Windows.Forms.ImageListStreamer)
        Me.ilPresence.TransparentColor = System.Drawing.Color.Transparent
        '
        'mnuPresence
        '
        Me.mnuPresence.MenuItems.AddRange(New System.Windows.Forms.MenuItem() {Me.mnuAvailable, Me.mnuAway, Me.MenuItem3, Me.mnuOffline})
        '
        'mnuAvailable
        '
        Me.mnuAvailable.Index = 0
        Me.mnuAvailable.Shortcut = System.Windows.Forms.Shortcut.CtrlO
        Me.mnuAvailable.Text = "Available"
        '
        'mnuAway
        '
        Me.mnuAway.Index = 1
        Me.mnuAway.Shortcut = System.Windows.Forms.Shortcut.CtrlA
        Me.mnuAway.Text = "Away"
        '
        'MenuItem3
        '
        Me.MenuItem3.Index = 2
        Me.MenuItem3.Text = "-"
        '
        'mnuOffline
        '
        Me.mnuOffline.Index = 3
        Me.mnuOffline.Shortcut = System.Windows.Forms.Shortcut.F9
        Me.mnuOffline.Text = "Offline"
        '
        'TabControl1
        '
        Me.TabControl1.Controls.Add(Me.tpRoster)
        Me.TabControl1.Controls.Add(Me.tpDebug)
        Me.TabControl1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TabControl1.Location = New System.Drawing.Point(0, 0)
        Me.TabControl1.Name = "TabControl1"
        Me.TabControl1.SelectedIndex = 0
        Me.TabControl1.Size = New System.Drawing.Size(632, 244)
        Me.TabControl1.TabIndex = 1
        '
        'tpRoster
        '
        Me.tpRoster.Controls.Add(Me.roster)
        Me.tpRoster.Location = New System.Drawing.Point(4, 22)
        Me.tpRoster.Name = "tpRoster"
        Me.tpRoster.Size = New System.Drawing.Size(624, 218)
        Me.tpRoster.TabIndex = 0
        Me.tpRoster.Text = "Roster"
        '
        'roster
        '
        Me.roster.Client = Me.jc
        Me.roster.Dock = System.Windows.Forms.DockStyle.Fill
        Me.roster.ImageIndex = 1
        Me.roster.Location = New System.Drawing.Point(0, 0)
        Me.roster.Name = "roster"
        Me.roster.PresenceManager = Me.pm
        Me.roster.RosterManager = Me.rm
        Me.roster.ShowLines = False
        Me.roster.ShowRootLines = False
        Me.roster.Size = New System.Drawing.Size(624, 218)
        Me.roster.Sorted = True
        Me.roster.TabIndex = 0
        '
        'tpDebug
        '
        Me.tpDebug.Controls.Add(Me.debug)
        Me.tpDebug.Location = New System.Drawing.Point(4, 22)
        Me.tpDebug.Name = "tpDebug"
        Me.tpDebug.Size = New System.Drawing.Size(624, 218)
        Me.tpDebug.TabIndex = 1
        Me.tpDebug.Text = "Debug"
        '
        'debug
        '
        Me.debug.Dock = System.Windows.Forms.DockStyle.Fill
        Me.debug.Location = New System.Drawing.Point(0, 0)
        Me.debug.Name = "debug"
        Me.debug.Size = New System.Drawing.Size(624, 218)
        Me.debug.TabIndex = 0
        Me.debug.Text = ""
        '
        'MainForm
        '
        Me.AutoScaleBaseSize = New System.Drawing.Size(5, 13)
        Me.ClientSize = New System.Drawing.Size(632, 266)
        Me.Controls.Add(Me.TabControl1)
        Me.Controls.Add(Me.sb)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "MainForm"
        Me.Text = "MainForm"
        CType(Me.pnlCon, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.pnlPresence, System.ComponentModel.ISupportInitialize).EndInit()
        Me.TabControl1.ResumeLayout(False)
        Me.tpRoster.ResumeLayout(False)
        Me.tpDebug.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub

#End Region

    Private Sub Connect()
        Dim log As New muzzle.ClientLogin(jc)
        log.ReadFromFile("login.xml")

        If log.ShowDialog() = Windows.Forms.DialogResult.OK Then
            log.WriteToFile("login.xml")
            jc.Connect()
        End If
    End Sub

    Private Sub sb_PanelClick(ByVal sender As System.Object, ByVal e As System.Windows.Forms.StatusBarPanelClickEventArgs) Handles sb.PanelClick
        If Not e.StatusBarPanel Is pnlPresence Then Return

        mnuPresence.Show(sb, New Point(e.X, e.Y))
    End Sub

    Private Sub mnuOffline_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles mnuOffline.Click
        If jc.IsAuthenticated Then
            jc.Close()
        End If
    End Sub

    Private Sub mnuAway_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles mnuAway.Click
        If jc.IsAuthenticated Then
            jc.Presence(PresenceType.available, "Away", "away", 0)
            pnlPresence.Text = "Away"
        Else
            Connect()
        End If
    End Sub

    Private Sub mnuAvailable_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles mnuAvailable.Click
        If jc.IsAuthenticated Then
            jc.Presence(PresenceType.available, "Available", Nothing, 0)
            pnlPresence.Text = "Available"
        Else
            Connect()
        End If
    End Sub

    Private Sub jc_OnConnect(ByVal sender As Object, ByVal stream As jabber.connection.StanzaStream) Handles jc.OnConnect
        m_err = False
        debug.AppendMaybeScroll("Connected to: " & stream.ToString() & vbCrLf)
    End Sub

    Private Sub jc_OnReadText(ByVal sender As Object, ByVal txt As String) Handles jc.OnReadText
        debug.SelectionColor = Color.Red
        debug.AppendText("RECV: ")
        debug.SelectionColor = Color.Black
        debug.AppendText(txt)
        debug.AppendMaybeScroll(vbCrLf)
    End Sub

    Private Sub jc_OnWriteText(ByVal sender As Object, ByVal txt As String) Handles jc.OnWriteText
        ' keepalive
        If txt = " " Then
            Return
        End If

        debug.SelectionColor = Color.Blue
        debug.AppendText("SEND: ")
        debug.SelectionColor = Color.Black
        debug.AppendText(txt)
        debug.AppendMaybeScroll(vbCrLf)
    End Sub

    Private Sub jc_OnAuthenticate(ByVal sender As Object) Handles jc.OnAuthenticate
        pnlPresence.Text = "Available"
        pnlCon.Text = "Connected"
    End Sub

    Private Sub jc_OnDisconnect(ByVal sender As Object) Handles jc.OnDisconnect
        pnlPresence.Text = "Offline"

        If Not m_err Then
            pnlCon.Text = "Disconnected"
        End If
    End Sub

    Private Sub jc_OnError(ByVal sender As Object, ByVal ex As System.Exception) Handles jc.OnError
        pnlCon.Text = "Error!"
        debug.SelectionColor = Color.Green
        debug.AppendText("ERROR: ")
        debug.SelectionColor = Color.Black
        debug.AppendText(ex.ToString())
        debug.AppendMaybeScroll(vbCrLf)
    End Sub

    Private Sub jc_OnAuthError(ByVal sender As Object, ByVal iq As jabber.protocol.client.IQ) Handles jc.OnAuthError
        If (MessageBox.Show(Me, "Create new account?", _
            "Authentication error", MessageBoxButtons.OKCancel) = Windows.Forms.DialogResult.OK) Then
            jc.Register(New JID(jc.User, jc.Server, Nothing))
        Else
            jc.Close()
            Connect()
        End If
    End Sub


    Private Sub jc_OnRegistered(ByVal sender As Object, ByVal iq As jabber.protocol.client.IQ) Handles jc.OnRegistered
        If (iq.Type = jabber.protocol.client.IQType.result) Then
            jc.Login()
        Else
            pnlCon.Text = "Registration error"
        End If
    End Sub

    Private Sub jc_OnRegisterInfo(ByVal sender As Object, ByVal iq As jabber.protocol.client.IQ) Handles jc.OnRegisterInfo
        Dim r As Register = DirectCast(iq.Query, Register)
        r.Password = jc.Password
    End Sub

    Private Sub jc_OnMessage(ByVal sender As Object, ByVal msg As jabber.protocol.client.Message) Handles jc.OnMessage
        MessageBox.Show(Me, msg.Body, msg.From.ToString(), MessageBoxButtons.OK)
    End Sub

    Private Sub jc_OnIQ(ByVal sender As Object, ByVal iq As jabber.protocol.client.IQ) Handles jc.OnIQ
        If iq.Type <> jabber.protocol.client.IQType.get Then Return

        If TypeOf iq.Query Is Version Then
            Dim ver As jabber.protocol.iq.Version = DirectCast(iq.Query, jabber.protocol.iq.Version)
            iq.Swap()
            iq.Type = jabber.protocol.client.IQType.result
            ver.OS = Environment.OSVersion.ToString()
            ver.EntityName = Application.ProductName
            ver.Ver = Application.ProductVersion
            jc.Write(iq)
        Else
            iq.Swap()
            iq.Type = jabber.protocol.client.IQType.error
            iq.Error.Code = jabber.protocol.client.ErrorCode.NOT_IMPLEMENTED
            jc.Write(iq)
        End If
    End Sub

    Private Sub rm_OnRosterEnd(ByVal sender As Object) Handles rm.OnRosterEnd
        roster.ExpandAll()
    End Sub

    Private Sub roster_DoubleClick(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Dim n As muzzle.RosterTree.ItemNode = DirectCast(roster.SelectedNode, muzzle.RosterTree.ItemNode)
        Dim sm As New SendMessage(jc, n.JID.ToString())
        sm.Show()
    End Sub

    Private Sub jc_OnStreamError(ByVal sender As Object, ByVal rp As System.Xml.XmlElement) Handles jc.OnStreamError
        m_err = True
        pnlCon.Text = "Stream error: " + rp.InnerText
    End Sub

    Private Sub jc_OnStreamInit(ByVal sender As Object, ByVal stream As jabber.protocol.ElementStream) Handles jc.OnStreamInit
        stream.AddFactory(New FooFactory)
    End Sub

    Private Sub MainForm_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles MyBase.Load
        bedrock.net.AsyncSocket.UntrustedRootOK = True
    End Sub

    Private Sub mnuPresence_Popup(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles mnuPresence.Popup

    End Sub
End Class

'-------------------------- Add packet type -----------------------
' don't forget to call AddFactory() in OnStreamInit!

' Convenience class, used for creating outbound IQ's with this type
Public Class FooIQ
    Inherits jabber.protocol.client.IQ

    Public Sub New(ByVal doc As XmlDocument)
        MyBase.New(doc)
        doc.AppendChild(New Foo(doc))
    End Sub
End Class

' The type of the first child of IQ.  Example packet:
'
' <iq>
'   <query xmlns='urn:foo'>
'     <bar>A value</bar>
'   </query>
' </iq>
Public Class Foo
    Inherits jabber.protocol.Element

    ' the namespace
    Public Const NS As String = "urn:foo"

    Public Sub New(ByVal doc As XmlDocument)
        MyBase.New("query", NS, doc)
    End Sub

    Public Sub New(ByVal prefix As String, ByVal qname As XmlQualifiedName, ByVal doc As XmlDocument)
        MyBase.New(prefix, qname, doc)
    End Sub

    ' this property gets and sets a child element called "bar".
    Public Property Bar() As String
        Get
            Return MyBase.GetElem("bar")
        End Get
        Set(ByVal Value As String)
            MyBase.SetElem("bar", Value)
        End Set
    End Property
End Class


' The factory class.  This ends up adding a mapping from urn:foo|foo to the constructor for the Foo class,
' under the covers.  The namespace|elementname of an inbound element will be looked up in the map to
' figure out how to create the correct type.
Public Class FooFactory
    Implements jabber.protocol.IPacketTypes

    Private Shared ReadOnly s_qnames As jabber.protocol.QnameType() = _
        {New jabber.protocol.QnameType("query", Foo.NS, GetType(Foo))}

    Public ReadOnly Property Types() As jabber.protocol.QnameType() Implements jabber.protocol.IPacketTypes.Types
        Get
            Return s_qnames
        End Get
    End Property
End Class
