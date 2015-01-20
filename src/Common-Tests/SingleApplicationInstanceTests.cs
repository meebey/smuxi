// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2015 Mirco Bauer <meebey@meebey.net>
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
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Threading;
using System.IO;

namespace Smuxi.Common
{
    [TestFixture]
    public class SingleApplicationInstanceTests
    {
        SingleApplicationInstance<TestApplication> FirstInstance { get; set; }

        class TestApplication : MarshalByRefObject
        {
            public int InvokeCounter { get; private set; }

            public void Invoke()
            {
                InvokeCounter++;
            }

            public override object InitializeLifetimeService()
            {
                // I want to live forever
                return null;
            }
        }

        [SetUp]
        public void SetUp()
        {
            FirstInstance = new SingleApplicationInstance<TestApplication>("test");
        }

        [TearDown]
        public void TearDown()
        {
            if (FirstInstance != null) {
                FirstInstance.Dispose();
            }
            try {
                var mutex = Mutex.OpenExisting("test");
                Assert.Fail();
            } catch (WaitHandleCannotBeOpenedException) {
            }
        }

        [Test]
        public void IsFirstInstance()
        {
            var instance1 = FirstInstance;
            Assert.IsTrue(instance1.IsFirstInstance);

            using (var instance2 = new SingleApplicationInstance<TestApplication>("test")) {
                Assert.IsFalse(instance2.IsFirstInstance);
            }
        }

        [Test]
        public void Dispose()
        {
            FirstInstance.Dispose();
            FirstInstance = null;

            try {
                var mutex = Mutex.OpenExisting("test");
                Assert.Fail();
            } catch (WaitHandleCannotBeOpenedException) {
            }
            Assert.IsNull(ChannelServices.GetChannel("ipc"));

            var appData = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData
            );
            var lockDirectory = Path.Combine(appData, "SingleApplicationInstance");
            var lockFile = Path.Combine(lockDirectory, "test");
            //Assert.IsFalse(File.Exists(lockFile));

            FirstInstance = new SingleApplicationInstance<TestApplication>("test");
            Assert.IsTrue(FirstInstance.IsFirstInstance);
            //Assert.IsTrue(File.Exists(lockFile));
        }

        [Test]
        public void Invoke()
        {
            var instance1 = FirstInstance;
            Assert.IsTrue(instance1.IsFirstInstance);

            instance1.FirstInstance = new TestApplication();
            Assert.IsNotNull(instance1.FirstInstance);
            Assert.AreEqual(0, instance1.FirstInstance.InvokeCounter);
            instance1.FirstInstance.Invoke();
            Assert.AreEqual(1, instance1.FirstInstance.InvokeCounter);

            using (var instance2 = new SingleApplicationInstance<TestApplication>("test")) {
                Assert.IsFalse(instance2.IsFirstInstance);

                Assert.IsNotNull(instance2.FirstInstance);
                Assert.AreEqual(1, instance2.FirstInstance.InvokeCounter);
                instance2.FirstInstance.Invoke();
                Assert.AreEqual(2, instance2.FirstInstance.InvokeCounter);
            }
        }
    }
}
