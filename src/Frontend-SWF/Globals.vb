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
Imports Meebey.Smuxi
Imports System.Windows.Forms


Structure ServerData
    Public Page As Engine.Page
    Public Network As Engine.INetworkManager
    Public Window As Form
End Structure
Module Globals
    Public FManager As Meebey.Smuxi.Engine.FrontendManager
    Public Session As Meebey.Smuxi.Engine.Session
    Public MainForm As MainForm
    Private ExitCode As Nullable(Of Integer) = Nothing


    Public Function Main(ByVal args As String()) As Integer
        MainForm = New MainForm()
        Do Until ExitCode.HasValue
            Application.DoEvents()
        Loop
        Return ExitCode
    End Function

    Public Sub Quit(Optional ByVal ExitCode As Integer = 0)
        Globals.ExitCode = ExitCode
    End Sub

End Module
