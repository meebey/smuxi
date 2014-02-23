// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2012 Mirco Bauer <meebey@meebey.net>
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
#if LIBGIT2SHARP
using System;
using System.IO;
using NUnit.Framework;
using Smuxi.Common;

namespace Smuxi.Engine
{
    [TestFixture]
    public class GitMessageBufferTests : MessageBufferTestsBase
    {
        protected override IMessageBuffer CreateBuffer()
        {
            var repoPath = Path.Combine(Platform.GetBuffersPath("testuser"),
                                        "testprot");
            repoPath = Path.Combine(repoPath, "testnet");
            repoPath = Path.Combine(repoPath, "testchat.git");
            if (Directory.Exists(repoPath)) {
                Directory.Delete(repoPath, true);
            }

            return OpenBuffer();
        }

        protected override IMessageBuffer OpenBuffer()
        {
            return new GitMessageBuffer("testuser", "testprot", "testnet", "testchat");
        }

    }
}
#endif
