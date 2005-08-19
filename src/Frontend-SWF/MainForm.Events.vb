't '*
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
    Public Sub ActivatePageForm(ByVal sender As Object, ByVal e As System.Windows.Forms.TreeNodeMouseClickEventArgs) Handles PageList.NodeMouseClick
        If e.Node.Tag Is Nothing Then Return
        Dim pf As PageForm = DirectCast(plist(e.Node.Tag), PageForm)
        If pf.WindowState = FormWindowState.Minimized Then pf.WindowState = FormWindowState.Normal
        pf.Show()
        pf.BringToFront()
    End Sub
    Private Sub PopulateNetworks(ByVal sender As Object, ByVal e As EventArgs) Handles NetworkBox.DropDown
        Dim s As String = NetworkBox.Text
        NetworkBox.Items.Clear()
        NetworkBox.Text = s
        For Each Network As Meebey.Smuxi.Engine.INetworkManager In Globals.Session.NetworkManagers
            NetworkBox.Items.Add(Network)
        Next
    End Sub

    Private Sub SelectNetwork(ByVal Sender As Object, ByVal e As EventArgs) Handles NetworkBox.SelectedIndexChanged
        Globals.FManager.CurrentNetworkManager = DirectCast(NetworkBox.SelectedItem, Meebey.Smuxi.Engine.INetworkManager)
    End Sub

    Public Sub ConnectSmuxi(ByVal sender As Object, ByVal e As EventArgs) Handles ConnectSmuxiItem.Click
        Dim smuxi As New ConnectSmuxi
        If smuxi.ShowDialog = Windows.Forms.DialogResult.OK Then
            Dim sp As New Runtime.Remoting.Channels.BinaryServerFormatterSinkProvider()
            Dim cp As New Runtime.Remoting.Channels.BinaryClientFormatterSinkProvider()
            Dim props As IDictionary = New Hashtable()
            Dim SManager As Meebey.Smuxi.Engine.SessionManager
            Dim URL As String
            props("port") = 0
            sp.TypeFilterLevel = Runtime.Serialization.Formatters.TypeFilterLevel.Full
            If ChannelServices.GetChannel("tcp") Is Nothing Then
                ChannelServices.RegisterChannel(New Tcp.TcpChannel(props, cp, sp))
            End If
            URL = String.Format("tcp://{0}:{1}/SessionManager", smuxi.Hostname, smuxi.Port)
            SManager = DirectCast(Activator.GetObject(GetType(SessionManager), URL), SessionManager)
            DisconnectSmuxi()
            Globals.Session = SManager.Register(smuxi.Username, smuxi.Password, UI)
            Globals.FManager = Session.GetFrontendManager(UI)
        End If
    End Sub



End Class
