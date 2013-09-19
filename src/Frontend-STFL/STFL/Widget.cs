// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2011 Mirco Bauer
// 
// Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
// 
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA

using System;

namespace Stfl
{
    public abstract class Widget
    {
        public string HeigthVariableName { get; set; }
        public string WidthVariableName { get; set; }
        protected Form Form { get; private set; }
        protected string WidgetName { get; set; }

        public int Heigth {
            get {
                Render();
                var variableName = HeigthVariableName;
                if (variableName == null) {
                    variableName = String.Format("{0}:h", WidgetName);
                }
                var value = Form[variableName];
                try {
                    return Int32.Parse(value);
                } catch (FormatException ex) {
                    throw new FormatException(
                        String.Format(
                            "Failed to parse Widget.Heigth: '{0}' as number " +
                            "(HeigthVariableName: '{1}').",
                            value, variableName
                        ),
                        ex
                    );
                }
            }
        }

        public int Width {
            get {
                Render();
                var variableName = WidthVariableName;
                if (variableName == null) {
                    variableName = String.Format("{0}:w", WidgetName);
                }
                var value = Form[variableName];
                try {
                    return Int32.Parse(value);
                } catch (FormatException ex) {
                    throw new FormatException(
                        String.Format(
                            "Failed to parse Widget.Width: '{0}' as number " +
                            "(WidthVariableName: '{1}').",
                            value, variableName
                        ),
                        ex
                    );
                }
            }
        }

        public int MinHeigth {
            get {
                Render();
                return Int32.Parse(Form[String.Format("{0}:minh", WidgetName)]);
            }
        }

        public int MinWidth {
            get {
                Render();
                return Int32.Parse(Form[String.Format("{0}:minw", WidgetName)]);
            }
        }

        public int XPosition {
            get {
                Render();
                return Int32.Parse(Form[String.Format("{0}:x", WidgetName)]);
            }
        }

        public int YPosition {
            get {
                Render();
                return Int32.Parse(Form[String.Format("{0}:y", WidgetName)]);
            }
        }

        protected Widget(Form form, string widgetName)
        {
            if (form == null) {
                throw new ArgumentNullException("form");
            }
            if (widgetName == null) {
                throw new ArgumentNullException("widgetName");
            }

            Form = form;
            WidgetName = widgetName;
        }

        public void Bind()
        {
            CheckWidget();
        }

        protected void Render()
        {
            Form.Run(-3);
        }

        protected bool WidgetExists()
        {
            return String.IsNullOrEmpty(Form.Dump(WidgetName, null, 0));
        }

        protected void CheckWidget()
        {
            if (!WidgetExists()) {
                return;
            }

            throw new ArgumentException(
                String.Format("Widget name: '{0}' is already used.", WidgetName)
            );
        }
    }
}
