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
