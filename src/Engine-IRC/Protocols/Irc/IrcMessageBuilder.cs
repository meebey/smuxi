// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2010 Mirco Bauer <meebey@meebey.net>
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
using System.Text.RegularExpressions;

namespace Smuxi.Engine
{
    public enum IrcControlCode : int {
        Bold      = 2,
        Color     = 3,
        Clear     = 15,
        Italic    = 26,
        Underline = 31,
    }

    public class IrcMessageBuilder : MessageBuilder
    {
        private static char[] IrcControlChars { get; set; }

        static IrcMessageBuilder()
        {
            int[] intValues = (int[])Enum.GetValues(typeof(IrcControlCode));
            char[] chars = new char[intValues.Length];
            int i = 0;
            foreach (int intValue in intValues) {
                chars[i++] = (char)intValue;
            }
            IrcControlChars = chars;
        }

        public override MessageBuilder AppendMessage(string msg)
        {
            msg = msg ?? "";

            if (msg.Length == 0) {
                return this;
            }

            // strip color and formatting if configured
            if (StripColors) {
                msg = Regex.Replace(msg, (char)IrcControlCode.Color +
                            "[0-9]{1,2}(,[0-9]{1,2})?", String.Empty);
            }
            if (StripFormattings) {
                msg = Regex.Replace(msg, String.Format("({0}|{1}|{2}|{3})",
                                                    (char)IrcControlCode.Bold,
                                                    (char)IrcControlCode.Clear,
                                                    (char)IrcControlCode.Italic,
                                                    (char)IrcControlCode.Underline), String.Empty);
            }

            // convert * / _ to mIRC control characters
            string[] messageParts = msg.Split(new char[] {' '});
            // better regex? \*([^ *]+)\*
            //string pattern = @"^({0})([A-Za-z0-9]+?){0}$";
            string pattern = @"^({0})([^ *]+){0}$";
            for (int i = 0; i < messageParts.Length; i++) {
                messageParts[i] = Regex.Replace(messageParts[i], String.Format(pattern, @"\*"), (char)IrcControlCode.Bold      + "$1$2$1" + (char)IrcControlCode.Bold);
                messageParts[i] = Regex.Replace(messageParts[i], String.Format(pattern,  "_"),  (char)IrcControlCode.Underline + "$1$2$1" + (char)IrcControlCode.Underline);
                messageParts[i] = Regex.Replace(messageParts[i], String.Format(pattern,  "/"),  (char)IrcControlCode.Italic    + "$1$2$1" + (char)IrcControlCode.Italic);
            }
            msg = String.Join(" ", messageParts);
            
            // crash: ^C^C0,7Dj Ler #Dj KanaL?na Girmek ZorunDaD?rLar UnutMay?N @>'^C0,4WwW.MaViGuL.NeT ^C4]^O ^C4]'
            // parse colors
            bool bold = false;
            bool underline = false;
            bool italic = false;
            bool color = false;
            TextColor fg_color = IrcTextColor.Normal;
            TextColor bg_color = IrcTextColor.Normal;
            bool controlCharFound;
            do {
                string submessage;
                int controlPos = msg.IndexOfAny(IrcControlChars);
                if (controlPos > 0) {
                    // control char found and we have normal text infront
                    controlCharFound = true;
                    submessage = msg.Substring(0, controlPos);
                    msg = msg.Substring(controlPos);
                } else if (controlPos != -1) {
                    // control char found
                    controlCharFound = true;
                    
                    char controlChar = msg.Substring(controlPos, 1)[0];
                    IrcControlCode controlCode = (IrcControlCode)controlChar;
                    string controlChars = controlChar.ToString();
                    switch (controlCode) {
                        case IrcControlCode.Clear:
#if LOG4NET && MSG_DEBUG
                            Logger.Debug("AppendMessage(): found clear control character");
#endif
                            bold = false;
                            underline = false;
                            italic = false;
                            
                            color = false;
                            fg_color = IrcTextColor.Normal;
                            bg_color = IrcTextColor.Normal;
                            break;
                        case IrcControlCode.Bold:
#if LOG4NET && MSG_DEBUG
                            Logger.Debug("AppendMessage(): found bold control character");
#endif
                            bold = !bold;
                            break;
                        case IrcControlCode.Underline:
#if LOG4NET && MSG_DEBUG
                            Logger.Debug("AppendMessage(): found underline control character");
#endif
                            underline = !underline;
                            break;
                        case IrcControlCode.Italic:
#if LOG4NET && MSG_DEBUG
                            Logger.Debug("AppendMessage(): found italic control character");
#endif
                            italic = !italic;
                            break;
                        case IrcControlCode.Color:
#if LOG4NET && MSG_DEBUG
                            Logger.Debug("AppendMessage(): found color control character");
#endif
                            color = !color;
                            string colorMessage = msg.Substring(controlPos);
#if LOG4NET && MSG_DEBUG
                            Logger.Debug("AppendMessage(): colorMessage: '" + colorMessage + "'");
#endif
                            Match match = Regex.Match(colorMessage, (char)IrcControlCode.Color + "(?<fg>[0-9][0-9]?)(,(?<bg>[0-9][0-9]?))?");
                            if (match.Success) {
                                controlChars = match.Value;
                                int color_code;
                                if (match.Groups["fg"] != null) {
#if LOG4NET && MSG_DEBUG
                                    Logger.Debug("AppendMessage(): match.Groups[fg].Value: " + match.Groups["fg"].Value);
#endif
                                    try {
                                        color_code = Int32.Parse(match.Groups["fg"].Value);
                                        fg_color = IrcColorToTextColor(color_code);
                                    } catch (FormatException) {
                                        fg_color = IrcTextColor.Normal;
                                    }
                                }
                                if (match.Groups["bg"] != null) {
#if LOG4NET && MSG_DEBUG
                                    Logger.Debug("AppendMessage(): match.Groups[bg].Value: " + match.Groups["bg"].Value);
#endif
                                    try {
                                        color_code = Int32.Parse(match.Groups["bg"].Value);
                                        bg_color = IrcColorToTextColor(color_code);
                                    } catch (FormatException) {
                                        bg_color = IrcTextColor.Normal;
                                    }
                                }
                            } else {
                                controlChars = controlChar.ToString();
                                fg_color = IrcTextColor.Normal;
                                bg_color = IrcTextColor.Normal;
                            }
#if LOG4NET && MSG_DEBUG
                            Logger.Debug("AppendMessage(): fg_color.HexCode: " + String.Format("0x{0:X6}", fg_color.HexCode));
                            Logger.Debug("AppendMessage(): bg_color.HexCode: " + String.Format("0x{0:X6}", bg_color.HexCode));
#endif
                            break;
                    }
#if LOG4NET && MSG_DEBUG
                    Logger.Debug("AppendMessage(): controlChars.Length: " + controlChars.Length);
#endif

                    // check if there are more control chars in the rest of the message
                    int nextControlPos = msg.IndexOfAny(IrcControlChars, controlPos + controlChars.Length);
                    if (nextControlPos != -1) {
                        // more control chars found
                        submessage = msg.Substring(controlChars.Length, nextControlPos - controlChars.Length);
                        msg = msg.Substring(nextControlPos);
                    } else {
                        // no next control char
                        // skip the control chars
                        submessage = msg.Substring(controlChars.Length);
                        msg = String.Empty;
                    }
                } else {
                    // no control char, nothing to do
                    controlCharFound = false;
                    submessage = msg;
                }
                
                TextMessagePartModel msgPart = new TextMessagePartModel();
                msgPart.Text = submessage;
                msgPart.Bold = bold;
                msgPart.Underline = underline;
                msgPart.Italic = italic;
                msgPart.ForegroundColor = fg_color;
                msgPart.BackgroundColor = bg_color;
                AppendText(msgPart);
            } while (controlCharFound);
            return this;
        }

        private static TextColor IrcColorToTextColor(int color)
        {
            switch (color) {
                case 0:
                    return IrcTextColor.White;
                case 1:
                    return IrcTextColor.Black;
                case 2:
                    return IrcTextColor.Blue;
                case 3:
                    return IrcTextColor.Green;
                case 4:
                    return IrcTextColor.Red;
                case 5:
                    return IrcTextColor.Brown;
                case 6:
                    return IrcTextColor.Purple;
                case 7:
                    return IrcTextColor.Orange;
                case 8:
                    return IrcTextColor.Yellow;
                case 9:
                    return IrcTextColor.LightGreen;
                case 10:
                    return IrcTextColor.Teal;
                case 11:
                    return IrcTextColor.LightCyan;
                case 12:
                    return IrcTextColor.LightBlue;
                case 13:
                    return IrcTextColor.LightPurple;
                case 14:
                    return IrcTextColor.Grey;
                case 15:
                    return IrcTextColor.LightGrey;
                default:
                    return IrcTextColor.Normal;
            }
        }
    }
}

