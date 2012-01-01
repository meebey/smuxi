/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2007 Mirco Bauer <meebey@meebey.net>
 *
 * Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
 */

using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("Smuxi - STFL frontend")]
[assembly: AssemblyCopyright("2007-2011 (C) Mirco Bauer <meebey@meebey.net>, 2011 (C) Andrius Bentkus <andrius.bentkus@gmail.com>")]

[assembly: AssemblyDelaySign(false)]
[assembly: AssemblyKeyFile("")]

#if LOG4NET
// let log4net use .exe.config file
[assembly: log4net.Config.XmlConfigurator]
#endif
