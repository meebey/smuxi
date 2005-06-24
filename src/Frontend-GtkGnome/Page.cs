/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005 Mirco Bauer <meebey@meebey.net>
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
using Meebey.Smuxi;

namespace Meebey.Smuxi.FrontendGtkGnome
{
    public abstract class Page : Gtk.EventBox
    {
        private   Engine.Page        _EnginePage;            
        private   int                _Number;
        protected Gtk.Label          _Label;
        protected Gtk.EventBox       _LabelEventBox;
        protected Gtk.ScrolledWindow _OutputScrolledWindow;
        protected Gtk.TextView       _OutputTextView;
    
        public Engine.Page EnginePage {
            get {
                return _EnginePage;
            }
        }
        
        public int Number {
            get {
                return _Number;
            }
            set {
                _Number = value;
            }
        }

        public Gtk.Label Label {
            get {
                return _Label;
            }
            set {
                _Label = value;
            }
        }
    
        public Gtk.EventBox LabelEventBox {
            get {
                return _LabelEventBox;
            }
        }
        
        public Gtk.TextView OutputTextView {
            get {
                return _OutputTextView;
            }
        }
        
        public Gtk.TextBuffer OutputTextBuffer {
            get {
                return _OutputTextView.Buffer;
            }
        }
        
        public Page(Engine.Page epage)
        {
            _EnginePage = epage;
            _LabelEventBox = new Gtk.EventBox();
            _LabelEventBox.VisibleWindow = false;
            Name = epage.Name;
            
            Gtk.TextView tv = new Gtk.TextView();
            tv.Editable = false;
            tv.CursorVisible = false;
            tv.WrapMode = Gtk.WrapMode.Word;
            tv.Buffer.Changed += new EventHandler(_OnTextBufferChanged);
            _OutputTextView = tv;
            
            Gtk.ScrolledWindow sw = new Gtk.ScrolledWindow();
            sw.HscrollbarPolicy = Gtk.PolicyType.Never;
            sw.VscrollbarPolicy = Gtk.PolicyType.Always;
            sw.ShadowType = Gtk.ShadowType.In;
            sw.Add(_OutputTextView);
            _OutputScrolledWindow = sw;
        }
    
        private void _OnTextBufferChanged(object obj, EventArgs args)
        {
#if LOG4NET
            Logger.UI.Debug("_OnTextBufferChanged triggered");
#endif
        
            Gtk.ScrolledWindow sw = _OutputScrolledWindow;
            Gtk.TextView tv = _OutputTextView;
            if (sw.Vadjustment.Upper == (sw.Vadjustment.Value + sw.Vadjustment.PageSize)) {
                // the scrollbar is way at the end, lets autoscroll
                Gtk.TextIter endit = tv.Buffer.EndIter;
                tv.Buffer.PlaceCursor(endit);
                tv.Buffer.MoveMark(tv.Buffer.InsertMark, endit);
                tv.ScrollMarkOnscreen(tv.Buffer.InsertMark);
            }
        }
    }
}
