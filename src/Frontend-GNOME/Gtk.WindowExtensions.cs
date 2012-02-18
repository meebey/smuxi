// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2012 Mirco Bauer <meebey@meebey.net>
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Runtime.InteropServices;

namespace Gtk.Extensions
{
    public static class WindowExtensions
    {
        static bool IsGdkX11Available = true;

        [DllImport("libgdk-x11-2.0.so.0")]
        static extern UInt32 gdk_x11_get_server_time(IntPtr gdkWindowHandle);

        public static void PresentWithServerTime(this Gtk.Window window)
        {
            if (window == null) {
                return;
            }
            var gdkWindow = window.GdkWindow;
            if (gdkWindow == null || !IsGdkX11Available) {
                window.Present();
                return;
            }

            // HACK: disabled, see window.AddEvents() below
            /*
            if ((gdkWindow.Events & Gdk.EventMask.PropertyChangeMask) == 0) {
                // GDK_PROPERTY_CHANGE_MASK is not set thus we have to bail out
                // else gdk_x11_get_server_time() will hang!
                window.Present();
                return;
            }
            */

            // HACK: we can't obtain and check for GDK_PROPERTY_CHANGE_MASK as
            // gdk_window_x11_get_events() filters that mask, thus we have to
            // ignorantly set it using gtk_widget_add_events() else
            // gdk_x11_get_server_time() would hang if it wasn't set!
            window.AddEvents((int) Gdk.EventMask.PropertyChangeMask);

            try {
                // TODO: should we fallback to gdk_x11_display_get_user_time?
                var timestamp = gdk_x11_get_server_time(gdkWindow.Handle);
                window.PresentWithTime(timestamp);
            } catch (DllNotFoundException) {
                IsGdkX11Available = false;
                // no libgdk-x11 available (probably Mac OS X or Windows), thus
                // fallback to gtk_window_present() without a timestamp as they
                // don't require a timestamp to change the window focus
                window.Present();
            }
        }
    }
}
