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
Public Class MainForm
    Implements Meebey.Smuxi.Engine.IFrontendUI

    Private plist As New System.Collections.Generic.Dictionary(Of Meebey.Smuxi.Engine.Page, PageForm)
    Private UI As New WrapUI(Me, AddressOf Me.invoke)


    Public ReadOnly Property Version() As Integer Implements Meebey.Smuxi.Engine.IFrontendUI.Version
        Get
            Return My.Application.Info.Version.Major
        End Get
    End Property

    Public Sub New()


        ' This call is required by the Windows Form Designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
#If CONFIG = "Debug" Then
        Meebey.Smuxi.Engine.Logger.Init()
#End If
        Meebey.Smuxi.Engine.Engine.Init()
        Me.Show()
        Globals.Session = New Meebey.Smuxi.Engine.Session(Meebey.Smuxi.Engine.Engine.Config, "local")
        Globals.Session.RegisterFrontendUI(UI)
        Globals.FManager = Session.GetFrontendManager(UI)
    End Sub

    Protected Overrides Sub OnClosing(ByVal e As System.ComponentModel.CancelEventArgs)
        e.Cancel = False
        MyBase.OnClosing(e)
    End Sub


    Private Function GetServerNode(ByVal servername As String) As TreeNode
        GetServerNode = PageList.Nodes(servername)
        If GetServerNode Is Nothing Then
            GetServerNode = PageList.Nodes.Add(servername, servername)
        End If
    End Function

End Class
