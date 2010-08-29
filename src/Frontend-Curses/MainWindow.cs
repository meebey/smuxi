/*
 * $Id: MainWindow.cs 192 2007-04-22 11:48:12Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/MainWindow.cs $
 * $Rev: 192 $
 * $Author: meebey $
 * $Date: 2007-04-22 13:48:12 +0200 (Sun, 22 Apr 2007) $
 *
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

using System;
using System.IO;
using System.Reflection;
using Mono.Unix;
using Mono.Terminal;
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Curses
{
    public class MainWindow : Container
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private Label    _NetworkStatusbar;
        private Label    _Statusbar;
        private CursesUI _UI;
        private Entry    _Entry;
        
        public CursesUI UI {
            get {
                return _UI;
            }
        }
        
        public Entry Entry {
            get {
                return _Entry;
            }
        }
        
        public MainWindow() : base(0, 0, Application.Cols, Application.Lines)
        {
        	//Frame layout = new Frame(0,0, Application.Cols, Application.Lines, "smuxi");
        	//Add(layout);
        	
        	// menu
        	Button fileButton = new Button(0, 0, "File");
        	fileButton.Clicked += delegate {
                Dialog dialog = new Dialog(40, 6, "File Menu");
                
    	        Button quitButton = new Button(0, 0, "Quit");
    	        quitButton.Clicked += delegate {
                    Frontend.Quit();
                };
                dialog.AddButton(quitButton);
                
    	        Button closeButton = new Button(0, 0, "Close");
    	        closeButton.Clicked += delegate {
    	            dialog.Running = false;
    	            dialog.Clear();
                };
                dialog.AddButton(closeButton);

                Application.Run(dialog);
        	};
    	    Add(fileButton);

        	Button helpButton = new Button(10, 0, "Help");
        	helpButton.Clicked += delegate {
                Dialog dialog = new Dialog(30, 6, "Help Menu");

    	        Button aboutButton = new Button(0, 0, "About");
    	        aboutButton.Clicked += delegate {
                    Dialog aboutDialog = new Dialog(70, 10, "About smuxi");
                    
                    aboutDialog.Add(new Label(0, 0, "Smuxi"));
                    aboutDialog.Add(new Label(0, 1, "Frontend: " + Frontend.UIName + " " + Frontend.Version));
                    aboutDialog.Add(new Label(0, 2, "Engine: " + Frontend.EngineVersion));
                    aboutDialog.Add(new Label(0, 4, "Copyright(C) 2005-2007 (C) Mirco Bauer <meebey@meebey.net>"));
                    
                    Button closeButton = new Button("Close");
                    closeButton.Clicked += delegate {
                        aboutDialog.Running = false;
                        aboutDialog.Clear();
                    };
                    aboutDialog.AddButton(closeButton);
                    
                    Application.Run(aboutDialog);
                };
                dialog.AddButton(aboutButton);

    	        Button helpCloseButton = new Button(0, 0, "Close");
    	        helpCloseButton.Clicked += delegate {
    	            dialog.Running = false;
    	            dialog.Clear();
                };
                dialog.AddButton(helpCloseButton);

                Application.Run(dialog);
        	};
        	Add(helpButton);
        	
    	    // output
        	/*
        	TextView textView = new TextView(0, 1, Application.Cols, Application.Lines -2);
        	textView.Add("Hello World!");
        	textView.Add("Foo bar me!");
        	Add(textView);
        	*/
        	LogWidget log = new LogWidget(0, 1, Application.Cols, Application.Lines -2);
        	Add(log);
        	
            _UI = new CursesUI(log);
            
        	// input
        	Entry entry = new Entry(0, Application.Lines - 1, Application.Cols, String.Empty);
        	Add(entry);
        	_Entry = entry;
        	
        	// status
        	
    	}
    }
}
