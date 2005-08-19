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
Imports System.Runtime.Remoting
Imports System.Runtime.Remoting.Channels

Partial Public Class MainForm
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
        If page.NetworkManager Is Nothing Then
            PageList.Nodes.Add(n)
        Else
            GetServerNode(page.NetworkManager.ToString).Nodes.Add(n)
        End If
        plist.Add(page, pf)
        pf.Tag = n
    End Sub

    Public Sub AddTextToPage(ByVal page As Meebey.Smuxi.Engine.Page, ByVal msg As Meebey.Smuxi.Engine.FormattedMessage) Implements Meebey.Smuxi.Engine.IFrontendUI.AddMessageToPage
        plist(page).AddMessage(msg)
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
        plist(cpage).UpdateTopic(topic)
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

End Class