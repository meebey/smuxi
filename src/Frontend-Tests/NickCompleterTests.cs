// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2013 Mirco Bauer <meebey@meebey.net>
// Copyright (c) 2013 Ondra Hosek <ondra.hosek@gmail.com>
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
using System.Collections.Generic;
using Smuxi.Engine;
using Smuxi.Frontend;
using NUnit.Framework;

namespace Smuxi.Frontend
{
    [TestFixture]
    public class NickCompleterTests
    {
        private LongestPrefixNickCompleter lpnc;
        private TabCycleNickCompleter tcnc;
        private MockChatView cv;

        public NickCompleterTests()
        {
        }

        [SetUp]
        public void Prepare()
        {
            lpnc = new LongestPrefixNickCompleter();
            lpnc.CompletionChar = ":";

            tcnc = new TabCycleNickCompleter();
            tcnc.CompletionChar = ":";

            cv = new MockChatView();
        }

        private void AssertNoMessagesOutput()
        {
            if (cv.MessageCount() > 0) {
                MessageModel lastMsg = cv.GetLastMessage();
                Console.Error.WriteLine(string.Join("; ", cv.ParticipantNicks()));
                Console.Error.WriteLine(lastMsg.ToString());
            }
            Assert.AreEqual(0, cv.MessageCount());
        }

        [Test]
        public void TestInitialUniqueBashCompletion()
        {
            cv.AddParticipant("Horatio");

            string inputLine = "Hor";
            int curPos = 3;
            lpnc.Complete(ref inputLine, ref curPos, cv);

            // completer did not output a message
            AssertNoMessagesOutput();

            // check new string and position
            Assert.AreEqual("Horatio: ", inputLine);
            Assert.AreEqual(9, curPos);
        }

        [Test]
        public void TestInitialUniqueAtBashCompletion()
        {
            cv.AddParticipant("Laertes");

            string inputLine = "@La";
            int curPos = 3;
            lpnc.Complete(ref inputLine, ref curPos, cv);

            AssertNoMessagesOutput();
            // no completion character!
            Assert.AreEqual("@Laertes ", inputLine);
            Assert.AreEqual(9, curPos);
        }

        [Test]
        public void TestFinalUniqueBashCompletion()
        {
            cv.AddParticipant("Polonius");

            string inputLine = "What says Pol";
            int curPos = 13;
            lpnc.Complete(ref inputLine, ref curPos, cv);

            AssertNoMessagesOutput();
            Assert.AreEqual("What says Polonius ", inputLine);
            Assert.AreEqual(19, curPos);
        }

        [Test]
        public void TestFinalUniqueAtBashCompletion()
        {
            cv.AddParticipant("Rosencrantz");

            string inputLine = "Welcome! @Ros";
            int curPos = 13;
            lpnc.Complete(ref inputLine, ref curPos, cv);

            AssertNoMessagesOutput();
            Assert.AreEqual("Welcome! @Rosencrantz ", inputLine);
            Assert.AreEqual(22, curPos);
        }

        [Test]
        public void TestMedialUniqueBashCompletion()
        {
            cv.AddParticipant("Ophelia");

            string inputLine = "How now, Op ! what's the matter?";
            int curPos = 11;
            lpnc.Complete(ref inputLine, ref curPos, cv);

            AssertNoMessagesOutput();
            Assert.AreEqual("How now, Ophelia ! what's the matter?", inputLine);
            Assert.AreEqual(16, curPos);
        }

        [Test]
        public void TestMedialUniqueAtBashCompletion()
        {
            cv.AddParticipant("Rosencrantz");

            string inputLine = "Welcome! @Ros @Guildenstern";
            int curPos = 13;
            lpnc.Complete(ref inputLine, ref curPos, cv);

            AssertNoMessagesOutput();
            Assert.AreEqual("Welcome! @Rosencrantz @Guildenstern", inputLine);
            Assert.AreEqual(21, curPos);
        }

        [Test]
        public void TestInitialPrefixBashCompletion()
        {
            cv.AddParticipant("Papagena");
            cv.AddParticipant("Papageno");

            string inputLine = "Pa";
            int curPos = 2;
            lpnc.Complete(ref inputLine, ref curPos, cv);

            Assert.AreEqual(1, cv.MessageCount());
            Assert.AreEqual("-!- Papagena Papageno", cv.GetLastMessage().ToString());
            Assert.AreEqual("Papagen", inputLine);
            Assert.AreEqual(7, curPos);
        }

        [Test]
        public void TestInitialPrefixAtBashCompletion()
        {
            cv.AddParticipant("Papagena");
            cv.AddParticipant("Papageno");

            string inputLine = "@Pa";
            int curPos = 3;
            lpnc.Complete(ref inputLine, ref curPos, cv);

            Assert.AreEqual(1, cv.MessageCount());
            Assert.AreEqual("-!- Papagena Papageno", cv.GetLastMessage().ToString());
            Assert.AreEqual("@Papagen", inputLine);
            Assert.AreEqual(8, curPos);
        }

        [Test]
        public void TestFinalPrefixBashCompletion()
        {
            cv.AddParticipant("Papagena");
            cv.AddParticipant("Papageno");

            string inputLine = "Dann wieder eine P";
            int curPos = 18;
            lpnc.Complete(ref inputLine, ref curPos, cv);

            Assert.AreEqual(1, cv.MessageCount());
            Assert.AreEqual("-!- Papagena Papageno", cv.GetLastMessage().ToString());
            Assert.AreEqual("Dann wieder eine Papagen", inputLine);
            Assert.AreEqual(24, curPos);
        }

        [Test]
        public void TestFinalPrefixAtBashCompletion()
        {
            cv.AddParticipant("Papagena");
            cv.AddParticipant("Papageno");

            string inputLine = "Halt ein und sei klug! @P";
            int curPos = 25;
            lpnc.Complete(ref inputLine, ref curPos, cv);

            Assert.AreEqual(1, cv.MessageCount());
            Assert.AreEqual("-!- Papagena Papageno", cv.GetLastMessage().ToString());
            Assert.AreEqual("Halt ein und sei klug! @Papagen", inputLine);
            Assert.AreEqual(31, curPos);
        }

        [Test]
        public void TestMedialPrefixBashCompletion()
        {
            cv.AddParticipant("Papagena");
            cv.AddParticipant("Papageno");

            string inputLine = "Ha! das ist Pap Ton.";
            int curPos = 15;
            lpnc.Complete(ref inputLine, ref curPos, cv);

            Assert.AreEqual(1, cv.MessageCount());
            Assert.AreEqual("-!- Papagena Papageno", cv.GetLastMessage().ToString());
            Assert.AreEqual("Ha! das ist Papagen Ton.", inputLine);
            Assert.AreEqual(19, curPos);
        }

        [Test]
        public void TestMedialPrefixAtBashCompletion()
        {
            cv.AddParticipant("Papagena");
            cv.AddParticipant("Papageno");

            string inputLine = "Du, @Pap , bist verloren.";
            int curPos = 8;
            lpnc.Complete(ref inputLine, ref curPos, cv);

            Assert.AreEqual(1, cv.MessageCount());
            Assert.AreEqual("-!- Papagena Papageno", cv.GetLastMessage().ToString());
            Assert.AreEqual("Du, @Papagen , bist verloren.", inputLine);
            Assert.AreEqual(12, curPos);
        }

        [Test]
        public void TestMultistepBashCompletion()
        {
            // also test sorting:
            cv.AddParticipant("Sarastro");
            cv.AddParticipant("Papageno");
            cv.AddParticipant("Pamina");
            cv.AddParticipant("Tamino");
            cv.AddParticipant("Papagena");

            string inputLine;
            int curPos;

            inputLine = "";
            curPos = 0;
            lpnc.Complete(ref inputLine, ref curPos, cv);

            Assert.AreEqual(1, cv.MessageCount());
            Assert.AreEqual("-!- Pamina Papagena Papageno Sarastro Tamino", cv.GetLastMessage().ToString());
            Assert.AreEqual("", inputLine);
            Assert.AreEqual(0, curPos);

            inputLine = "P";
            curPos = 1;
            lpnc.Complete(ref inputLine, ref curPos, cv);

            Assert.AreEqual(2, cv.MessageCount());
            Assert.AreEqual("-!- Pamina Papagena Papageno", cv.GetLastMessage().ToString());
            Assert.AreEqual("Pa", inputLine);
            Assert.AreEqual(2, curPos);

            inputLine = "Pap";
            curPos = 3;
            lpnc.Complete(ref inputLine, ref curPos, cv);

            Assert.AreEqual(3, cv.MessageCount());
            Assert.AreEqual("-!- Papagena Papageno", cv.GetLastMessage().ToString());
            Assert.AreEqual("Papagen", inputLine);
            Assert.AreEqual(7, curPos);

            inputLine = "Papagena";
            curPos = 8;
            lpnc.Complete(ref inputLine, ref curPos, cv);

            // unique match; no added message
            Assert.AreEqual(3, cv.MessageCount());
            Assert.AreEqual("Papagena: ", inputLine);
            Assert.AreEqual(10, curPos);
        }

        [Test]
        public void TestInitialMultipleIrssiCompletion() {
            // also test sorting:
            cv.AddParticipant("Carla");
            cv.AddParticipant("Timothy");
            cv.AddParticipant("Thomas");
            cv.AddParticipant("Claire");
            cv.AddParticipant("Theodore");

            string inputLine = "T";
            int curPos = 1;
            tcnc.Complete(ref inputLine, ref curPos, cv);

            AssertNoMessagesOutput();
            Assert.AreEqual("Theodore: ", inputLine);
            Assert.AreEqual(10, curPos);

            tcnc.Complete(ref inputLine, ref curPos, cv);

            AssertNoMessagesOutput();
            Assert.AreEqual("Thomas: ", inputLine);
            Assert.AreEqual(8, curPos);

            tcnc.Complete(ref inputLine, ref curPos, cv);

            AssertNoMessagesOutput();
            Assert.AreEqual("Timothy: ", inputLine);
            Assert.AreEqual(9, curPos);

            tcnc.Complete(ref inputLine, ref curPos, cv);

            AssertNoMessagesOutput();
            Assert.AreEqual("Theodore: ", inputLine);
            Assert.AreEqual(10, curPos);
        }

        [Test]
        public void TestFinalMultipleIrssiCompletion() {
            cv.AddParticipant("Carla");
            cv.AddParticipant("Claire");
            cv.AddParticipant("Theodore");
            cv.AddParticipant("Thomas");
            cv.AddParticipant("Timothy");

            string inputLine = "Who is T";
            int curPos = 8;

            tcnc.Complete(ref inputLine, ref curPos, cv);

            AssertNoMessagesOutput();
            Assert.AreEqual("Who is Theodore ", inputLine);
            Assert.AreEqual(16, curPos);

            tcnc.Complete(ref inputLine, ref curPos, cv);

            AssertNoMessagesOutput();
            Assert.AreEqual("Who is Thomas ", inputLine);
            Assert.AreEqual(14, curPos);

            tcnc.Complete(ref inputLine, ref curPos, cv);

            AssertNoMessagesOutput();
            Assert.AreEqual("Who is Timothy ", inputLine);
            Assert.AreEqual(15, curPos);

            tcnc.Complete(ref inputLine, ref curPos, cv);

            AssertNoMessagesOutput();
            Assert.AreEqual("Who is Theodore ", inputLine);
            Assert.AreEqual(16, curPos);
        }

        [Test]
        public void TestMedialMultipleIrssiCompletion() {
            cv.AddParticipant("Carla");
            cv.AddParticipant("Claire");
            cv.AddParticipant("Theodore");
            cv.AddParticipant("Thomas");
            cv.AddParticipant("Timothy");

            string inputLine = "Is T here?";
            int curPos = 4;

            tcnc.Complete(ref inputLine, ref curPos, cv);

            AssertNoMessagesOutput();
            Assert.AreEqual("Is Theodore here?", inputLine);
            Assert.AreEqual(11, curPos);

            tcnc.Complete(ref inputLine, ref curPos, cv);

            AssertNoMessagesOutput();
            Assert.AreEqual("Is Thomas here?", inputLine);
            Assert.AreEqual(9, curPos);

            tcnc.Complete(ref inputLine, ref curPos, cv);

            AssertNoMessagesOutput();
            Assert.AreEqual("Is Timothy here?", inputLine);
            Assert.AreEqual(10, curPos);

            tcnc.Complete(ref inputLine, ref curPos, cv);

            AssertNoMessagesOutput();
            Assert.AreEqual("Is Theodore here?", inputLine);
            Assert.AreEqual(11, curPos);
        }

        [Test]
        public void TestNoEmptyIrssiCompletion() {
            cv.AddParticipant("Escalus");
            cv.AddParticipant("Paris");
            cv.AddParticipant("Mercutio");
            cv.AddParticipant("Benvolio");

            string inputLine = "";
            int curPos = 0;

            tcnc.Complete(ref inputLine, ref curPos, cv);

            AssertNoMessagesOutput();
            Assert.AreEqual("", inputLine);
            Assert.AreEqual(0, curPos);
        }
    }
}

