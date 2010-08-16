// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2010 David Paleino <dapal@debian.org>
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
using NUnit.Framework;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    [TestFixture]
    public class PangoToolsTests
    {
        [Test]
        public void ToMarkup()
        {
            MessageModel testmodel = new MessageModel();
            testmodel.IsCompactable = false;
            TextMessagePartModel textmodel;
            UrlMessagePartModel urlmodel;

            textmodel = new TextMessagePartModel("normal");
            testmodel.MessageParts.Add(textmodel);

            textmodel = new TextMessagePartModel("blue");
            textmodel.ForegroundColor = TextColor.Parse("0000FF");
            testmodel.MessageParts.Add(textmodel);

            textmodel = new TextMessagePartModel("bold");
            textmodel.Bold = true;
            testmodel.MessageParts.Add(textmodel);

            textmodel = new TextMessagePartModel("bold2");
            textmodel.Bold = true;
            testmodel.MessageParts.Add(textmodel);

            textmodel = new TextMessagePartModel("normal");
            testmodel.MessageParts.Add(textmodel);

            textmodel = new TextMessagePartModel("underline");
            textmodel.Underline = true;
            testmodel.MessageParts.Add(textmodel);

            textmodel = new TextMessagePartModel("combined");
            textmodel.Underline = true;
            textmodel.Bold = true;
            textmodel.Italic = true;
            textmodel.ForegroundColor = TextColor.Parse("00FF00");
            textmodel.BackgroundColor = TextColor.Parse("0000FF");
            testmodel.MessageParts.Add(textmodel);

            urlmodel = new UrlMessagePartModel("http://www.smuxi.org");
            testmodel.MessageParts.Add(urlmodel);

            textmodel = new TextMessagePartModel("normal");
            testmodel.MessageParts.Add(textmodel);

            string expected = "normal<span color='#0000FF'>blue</span>" +
                "<b>bold</b><b>bold2</b>normal<u>underline</u>" +
                "<span color='#00FF00'><u><b><i>combined</i></b></u></span>" +
                "<span color='#00008B'><u>http://www.smuxi.org</u></span>normal";
            string tested = PangoTools.ToMarkup(testmodel);

            Assert.AreEqual(expected, tested);
        }
    }
}
