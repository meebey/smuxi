/*
 * $Id: MainWindow.cs 192 2007-04-22 11:48:12Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/MainWindow.cs $
 * $Rev: 192 $
 * $Author: meebey $
 * $Date: 2007-04-22 13:48:12 +0200 (Sun, 22 Apr 2007) $
 *
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2006 Mirco Bauer <meebey@meebey.net>
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

using System;
using System.IO;
using System.Reflection;
using Mono.Unix;
using Mono.Terminal;
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Curses
{
    public class LogWidget : Widget {
    	string [] messages = new string [80];
    	int head, tail;
    	int count;
    	
    	public LogWidget (int x, int y, int w, int h) : base (x, y, w, h)
    	{
    		//Fill = Fill.Horizontal | Fill.Vertical;
			AddText ("Started");
		}

		public void AddText (string s)
		{
			messages [head] = s;
			head++;
			if (head == messages.Length)
				head = 0;
			if (head == tail)
				tail = (tail+1) % messages.Length;
		}
		
		public override void Redraw ()
		{
			Mono.Terminal.Curses.attrset(ColorNormal);

			int i = 0;
			int l;
			int n = head > tail ? head-tail : (head + messages.Length) - tail;
			for (l = h-1; l >= 0 && n-- > 0; l--){
				int item = head-1-i;
				if (item < 0)
					item = messages.Length+item;

				Move (y+l, x);

				int sl = messages [item].Length;
				if (sl < w){
					Mono.Terminal.Curses.addstr (messages [item]);
					for (int fi = 0; fi < w-sl; fi++)
						Mono.Terminal.Curses.addch (' ');
				} else {
					Mono.Terminal.Curses.addstr (messages [item].Substring (0, sl));
				}
				i++;
			}

			for (; l >= 0; l--) {
				Move (y+l, x);
				for (i = 0; i < w; i++)
					Mono.Terminal.Curses.addch (' ');
			}
		}
	}
}
