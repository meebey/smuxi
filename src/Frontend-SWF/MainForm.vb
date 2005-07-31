Public Class MainForm
    Implements Meebey.Smuxi.Engine.IFrontendUI

    Private plist As New System.Collections.Generic.Dictionary(Of Meebey.Smuxi.Engine.Page, PageForm)
    Private UI As New WrapUI(Me, AddressOf Me.invoke)

    Public Sub ActivatePageForm(ByVal sender As Object, ByVal e As System.Windows.Forms.TreeNodeMouseClickEventArgs) Handles PageList.NodeMouseClick
        Dim pf As PageForm = DirectCast(plist(e.Node.Tag), PageForm)
        If pf.WindowState = FormWindowState.Minimized Then pf.WindowState = FormWindowState.Normal
        pf.Show()
        pf.BringToFront()
    End Sub

    Public Sub AddPage(ByVal page As Meebey.Smuxi.Engine.Page) Implements Meebey.Smuxi.Engine.IFrontendUI.AddPage

        Dim pf As PageForm
        Dim n As System.Windows.Forms.TreeNode
        Select Case page.PageType
            Case Meebey.Smuxi.Engine.PageType.Server
                pf = New ServerPageForm(page)
            Case Meebey.Smuxi.Engine.PageType.Channel
                pf = New ChannelPageForm(DirectCast(page, Meebey.Smuxi.Engine.ChannelPage))
            Case Meebey.Smuxi.Engine.PageType.Query
                pf = New QueryPageForm(page)
            Case Else
                Throw New Exception("Invalid Page Type")
        End Select
        pf.MdiParent = Me
        pf.Text = page.Name
        pf.Show()
        n = New System.Windows.Forms.TreeNode(page.Name)
        n.Tag = page
        PageList.Nodes.Add(n)
        plist.Add(page, pf)
        pf.Tag = n
    End Sub

    Public Sub AddTextToPage(ByVal page As Meebey.Smuxi.Engine.Page, ByVal text As String) Implements Meebey.Smuxi.Engine.IFrontendUI.AddTextToPage
        plist(page).AddText(text)
    End Sub

    Public Sub AddUserToChannel(ByVal cpage As Meebey.Smuxi.Engine.ChannelPage, ByVal user As Meebey.Smuxi.Engine.User) Implements Meebey.Smuxi.Engine.IFrontendUI.AddUserToChannel
        DirectCast(plist(cpage), ChannelPageForm).AddUser(user)
    End Sub

    Public Sub RemovePage(ByVal page As Meebey.Smuxi.Engine.Page) Implements Meebey.Smuxi.Engine.IFrontendUI.RemovePage
        Dim temppf As PageForm = plist(page)
        plist.Remove(page)
        DirectCast(temppf.Tag, TreeNode).Tag = Nothing
        PageList.Nodes.Remove(DirectCast(temppf.Tag, TreeNode))
        temppf.Tag = Nothing
        temppf.Close()
    End Sub

    Public Sub RemoveUserFromChannel(ByVal cpage As Meebey.Smuxi.Engine.ChannelPage, ByVal user As Meebey.Smuxi.Engine.User) Implements Meebey.Smuxi.Engine.IFrontendUI.RemoveUserFromChannel
        DirectCast(plist(cpage), ChannelPageForm).RemoveUser(user)
    End Sub

    Public Sub SetNetworkStatus(ByVal status As String) Implements Meebey.Smuxi.Engine.IFrontendUI.SetNetworkStatus
        Me.Status.Text = status
    End Sub

    Public Sub SetStatus(ByVal status As String) Implements Meebey.Smuxi.Engine.IFrontendUI.SetStatus
        Me.Status.Text = status
    End Sub

    Public Sub SyncPage(ByVal page As Meebey.Smuxi.Engine.Page) Implements Meebey.Smuxi.Engine.IFrontendUI.SyncPage
        plist(page).Sync()
    End Sub

    Public Sub UpdateTopicInChannel(ByVal cpage As Meebey.Smuxi.Engine.ChannelPage, ByVal topic As String) Implements Meebey.Smuxi.Engine.IFrontendUI.UpdateTopicInChannel
        plist(cpage).AddText(String.Format("Topic updated to ""{0}""", topic))
    End Sub

    Public Sub UpdateUserInChannel(ByVal cpage As Meebey.Smuxi.Engine.ChannelPage, ByVal olduser As Meebey.Smuxi.Engine.User, ByVal newuser As Meebey.Smuxi.Engine.User) Implements Meebey.Smuxi.Engine.IFrontendUI.UpdateUserInChannel
        DirectCast(plist(cpage), ChannelPageForm).RemoveUser(olduser)
        DirectCast(plist(cpage), ChannelPageForm).AddUser(newuser)
    End Sub

    Public ReadOnly Property Version() As Integer Implements Meebey.Smuxi.Engine.IFrontendUI.Version
        Get
            Return My.Application.Info.Version.Major
        End Get
    End Property

    Public Sub New()


        ' This call is required by the Windows Form Designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        Meebey.Smuxi.Engine.Logger.Init()
        Meebey.Smuxi.Engine.Engine.Init()
        Me.Show()
        Globals.Session = Meebey.Smuxi.Engine.Engine.SessionManager.Register("local", "smuxi", UI)
        Globals.FManager = Session.GetFrontendManager(UI)
    End Sub

    Protected Overrides Sub OnClosing(ByVal e As System.ComponentModel.CancelEventArgs)
        e.Cancel = False
        MyBase.OnClosing(e)
    End Sub

    Private Sub PopulateNetworks(ByVal sender As Object, ByVal e As EventArgs) Handles NetworkBox.DropDown
        NetworkBox.Items.Clear()
        For Each Network As Meebey.Smuxi.Engine.INetworkManager In Globals.Session.NetworkManagers
            NetworkBox.Items.Add(Network)
        Next
    End Sub

    Private Sub SelectNetwork(ByVal Sender As Object, ByVal e As EventArgs) Handles NetworkBox.SelectedIndexChanged
        Globals.FManager.CurrentNetworkManager = DirectCast(NetworkBox.SelectedItem, Meebey.Smuxi.Engine.INetworkManager)
    End Sub

End Class
