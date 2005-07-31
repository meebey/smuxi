Public Class ChannelPageForm
    Private ChannelPage As Meebey.Smuxi.Engine.ChannelPage

    Public Sub New(ByVal ChannelPage As Meebey.Smuxi.Engine.ChannelPage)
        MyBase.New(ChannelPage)
        Me.ChannelPage = ChannelPage


        ' This call is required by the Windows Form Designer.
        InitializeComponent()

        Me.Controls.SetChildIndex(Me.Userlist, 1)
        Me.Controls.SetChildIndex(Me.Splitter1, 1)

        ' Add any initialization after the InitializeComponent() call.
    End Sub

    Public Sub AddUser(ByVal User As Meebey.Smuxi.Engine.User)
        Userlist.Items.Add(User)
    End Sub

    Public Sub RemoveUser(ByVal User As Meebey.Smuxi.Engine.User)
        Userlist.Items.Remove(User)
    End Sub

    Public Overrides Sub Sync()
        Me.SuspendLayout()
        For Each user As Meebey.Smuxi.Engine.User In ChannelPage.Users.Values
            AddUser(user)
        Next
        Me.ResumeLayout()

        MyBase.Sync()
    End Sub

End Class