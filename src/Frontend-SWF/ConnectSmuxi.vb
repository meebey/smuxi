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
Public Class ConnectSmuxi

    Private HostNameValue As String
    Private PortValue As Integer
    Private UsernameValue As String
    Private PasswordValue As String

    Public ReadOnly Property Hostname() As String
        Get
            Return HostNameValue
        End Get
    End Property

    Public ReadOnly Property Port() As Integer
        Get
            Return PortValue
        End Get
    End Property

    Public ReadOnly Property Username() As String
        Get
            Return UsernameValue
        End Get
    End Property

    Public ReadOnly Property Password() As String
        Get
            Return PasswordValue
        End Get
    End Property


    Private Sub ValidatePort(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles PasswordText.Validating
        If Not IsNumeric(PortText.Text) Then e.Cancel = True
    End Sub

    Private Sub Okay(ByVal sender As Object, ByVal e As EventArgs) Handles OK_Button.Click
        HostNameValue = HostNameText.Text
        PortValue = PortText.Text
        UsernameValue = UsernameText.Text
        PasswordValue = PasswordText.Text
    End Sub

End Class
