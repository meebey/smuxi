/*
 * $Id: ChannelPage.cs 138 2006-12-23 17:11:57Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/ChannelPage.cs $
 * $Rev: 138 $
 * $Author: meebey $
 * $Date: 2006-12-23 18:11:57 +0100 (Sat, 23 Dec 2006) $
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
using System.Collections.Generic;
using System.Windows.Forms;
using Mono.Unix;
using Smuxi.Engine;

namespace Smuxi.Frontend.Swf
{
    [ChatViewInfo(ChatType = ChatType.Group)]
    public class GroupChatView : ChatView
    {
        private TextBox _TopicTextView;
        private ListBox _PersonListBox;

        public GroupChatView(ChatModel chat) :
                        base(chat)
        {
            InitializeComponents();

        }


        public GroupChatModel GroupChatModel {
            get {
                return (GroupChatModel)base.ChatModel;
            }
        }

        private void InitializeComponents()
        {
            Splitter personListBoxSplitter = new Splitter();
            this._TopicTextView = new TextBox();
            this._PersonListBox = new ListBox();
            this.SuspendLayout();

            // _TopicTextView
            this._TopicTextView.ReadOnly = true;
            this._TopicTextView.Name = "_TopicTextView";
            this._TopicTextView.Dock = DockStyle.Top;

            // _PersonListBox
            this._PersonListBox.Name = "_PersonListBox";
            this._PersonListBox.Dock = DockStyle.Right;
            this._PersonListBox.IntegralHeight = false;

            // personListBoxSplitter
            personListBoxSplitter.Dock = DockStyle.Right;

            this.Controls.Add(base.OutputTextView);
            this.Controls.Add(_TopicTextView);
            this.Controls.Add(personListBoxSplitter);
            this.Controls.Add(_PersonListBox);

            this.ResumeLayout();

        }

        public override void ApplyConfig(UserConfig config)
        {
            base.ApplyConfig(config);
            if (BackgroundColor.HasValue) _PersonListBox.BackColor = _TopicTextView.BackColor = BackgroundColor.Value;
            if (BackgroundColor.HasValue) _PersonListBox.ForeColor = _TopicTextView.ForeColor = ForegroundColor.Value;
            _PersonListBox.Font = _TopicTextView.Font = Font;
            _PersonListBox.Width = TextRenderer.MeasureText("999999999", Font).Width;
        }

        public void AddPerson(PersonModel person)
        {
            _PersonListBox.Items.Add(person.IdentityName);
        }

        public void UpdatePerson(PersonModel oldPerson, PersonModel newPerson)
        {
            _PersonListBox.Items.Remove(oldPerson.IdentityName);
            _PersonListBox.Items.Add(newPerson.IdentityName);
        }

        public void RemovePerson(PersonModel person)
        {
            _PersonListBox.Items.Remove(person.IdentityName);
        }

        public override void Sync()
        {
            base.Sync();
            var persons = GroupChatModel.Persons;
            if (persons == null) {
                persons = new Dictionary<string, PersonModel>(0);
            }
            foreach (PersonModel person in persons.Values) {
                _PersonListBox.Items.Add(person.IdentityName);
            }
        }

        public override void Disable()
        {
            base.Disable();
            _PersonListBox.Items.Clear();
            _TopicTextView.Clear();
        }

        public override IList<PersonModel> Participants {
            get {
                var ret = new List<PersonModel>();
                foreach (PersonModel person in GroupChatModel.Persons.Values) {
                    ret.Add(person);
                }
                return ret;
            }
        }
    }
}
