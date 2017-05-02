// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2014, 2017 Mirco Bauer <meebey@meebey.net>
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
using System.IO;
using System.Linq;
using System.Reflection;
using NDesk.Options;
using ServiceStack.Text;
using Smuxi.Common;
using Smuxi.Engine.Dto;
using System.Collections.Generic;

namespace Smuxi.Engine
{
    public class MainClass
    {
        static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static readonly string LibraryTextDomain = "smuxi-message-buffer";

        public static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.Name = "Main";

            // initialize log level
            log4net.Repository.ILoggerRepository repo = log4net.LogManager.GetRepository();
            repo.Threshold = log4net.Core.Level.Error;

            InitLocale();

            var debug = false;
            var parser = new OptionSet() {
                { "d|debug", _("Enable debug output"),
                    val => {
                        debug = true;
                    }
                }
            };
            parser.Add("h|help", _("Show this help"),
                val => {
                    ShowUsage(parser);
                    Environment.Exit(0);
                }
            );

            try {
                var mainArgs = args.TakeWhile(x => x.StartsWith("-"));
                parser.Parse(mainArgs);
                if (debug) {
                    repo.Threshold = log4net.Core.Level.Debug;
                }

                var action = args.Skip(mainArgs.Count()).FirstOrDefault();
                if (String.IsNullOrEmpty(action)) {
                    ShowUsage(parser);
                    Environment.Exit(1);
                }

                var actionArgs = args.Skip(mainArgs.Count() + 1);
                switch (action.ToLower()) {
                    case "cat":
                        CatAction(action, actionArgs);
                        break;
                    case "convert":
                    case "copy":
                    case "cp":
                        CopyAction(action, actionArgs);
                        break;
                    default:
                        throw new OptionException(
                            String.Format(
                                _("Unknown action: '{0}'"),
                                action
                            ),
                            "action"
                        );
                }
            } catch (OptionException ex) {
                Console.Error.WriteLine(_("Command line error: {0}"), ex.Message);
                Environment.Exit(1);
            } catch (Exception e) {
                Logger.Fatal(e);
            }
        }

        static void ShowUsage(OptionSet mainOptions)
        {
            Console.WriteLine(_("Usage: smuxi-message-buffer [options] action action-options"));
            Console.WriteLine();
            Console.WriteLine(_("Actions:"));
            Console.WriteLine("  cat");
            Console.WriteLine("  convert/copy/cp");
            Console.WriteLine();
            Console.WriteLine(_("Options:"));
            mainOptions.WriteOptionDescriptions(Console.Out);
        }

        static void CatAction(string action, IEnumerable<string> args)
        {
            var dbFormat = "";
            var parameters = new List<string>();
            var parser = new OptionSet() {
                { "format=", _("Database format (valid values: auto, db4o, sqlite)"),
                    val => {
                        if (val == "auto") {
                            val = "";
                        }
                        dbFormat = val;
                    }
                },
                { "<>",
                    val => {
                        if (!val.StartsWith("-")) {
                            parameters.Add(val);
                            return;
                        }
                        throw new OptionException(
                            String.Format(_("Unknown {0} option: '{1}'"),
                                          action, val),
                            val
                        );
                    }
                }
            };
            parser.Add("h|help", _("Show this help"),
                val => {
                    Console.WriteLine(
                        String.Format(
                            _("Usage: smuxi-message-buffer {0} [action-options] db_path"),
                            action
                        )
                    );
                    Console.WriteLine();
                    Console.WriteLine("  db_path(s)... " + _("Database path(s)"));
                    Console.WriteLine();
                    Console.WriteLine(_("Options:"));
                    parser.WriteOptionDescriptions(Console.Out);
                    Environment.Exit(0);
                }
            );

            parser.Parse(args);
            if (parameters.Count < 1) {
                throw new OptionException(
                    _("db_path is required"),
                    action
                );
            }
            var dbPaths = parameters.ToArray();
            Copy(dbPaths, dbFormat, null, null);
        }

        static void CopyAction(string action, IEnumerable<string> args)
        {
            var sourceFormat = "";
            var destinationFormat = "";
            var parameters = new List<string>();
            var parser = new OptionSet() {
                { "source-format=", _("Source format (valid values: auto, db4o, sqlite)"),
                    val => {
                        if (val == "auto") {
                            val = "";
                        }
                        sourceFormat = val;
                    }
                },
                { "destination-format=", _("Destination format (valid values: auto, db4o, sqlite)"),
                    val => {
                        if (val == "auto") {
                            val = "";
                        }
                        destinationFormat = val;
                    }
                },
                { "<>",
                    val => {
                        if (!val.StartsWith("-")) {
                            parameters.Add(val);
                            return;
                        }
                        throw new OptionException(
                            String.Format(_("Unknown {0} option: '{1}'"),
                                      action, val),
                            val
                        );
                    }
                }
            };
            parser.Add("h|help", _("Show this help"),
                val => {
                    Console.WriteLine(
                        String.Format(
                            _("Usage: smuxi-message-buffer {0} [action-options] source_db(s)... destination_db"),
                            action
                        )
                    );
                    Console.WriteLine();
                    Console.WriteLine("  source_db(s)... " + _("Source file path(s)"));
                    Console.WriteLine("  destination_db " + _("Destination file path or -/empty for stdout"));
                    Console.WriteLine();
                    Console.WriteLine(_("Options:"));
                    parser.WriteOptionDescriptions(Console.Out);
                    Environment.Exit(0);
                }
            );

            parser.Parse(args);
            if (parameters.Count < 2) {
                throw new OptionException(
                    _("source_db and destination_db are required"),
                    action
                );
            }
            var sourceFiles = parameters.Take(parameters.Count - 1).ToArray();
            var destinationFile = parameters.Last();
            if (destinationFile == "-") {
                destinationFile = "";
            }
            Copy(sourceFiles, sourceFormat, destinationFile, destinationFormat);
        }

        static void Copy(string sourceFile, string sourceFormat,
                         string destinationFile, string destinationFormat)
        {
            if (String.IsNullOrEmpty(sourceFile)) {
                throw new ArgumentException(_("sourceFile must not be empty."));
            }
            Copy(new string[] { sourceFile }, sourceFormat, destinationFile, destinationFormat);
        }

        static void Copy(string[] sourceFiles, string sourceFormat,
                         string destinationFile, string destinationFormat)
        {
            if (sourceFiles == null || sourceFiles.Length == 0) {
                throw new ArgumentException(_("sourceFiles must not be empty."));
            }

            var sourceBuffers = new List<IMessageBuffer>();
            IMessageBuffer destinationBuffer = null;
            try {
                foreach (var sourceFile in sourceFiles) {
                    var sourceBufferType = ParseMessageBufferType(sourceFile, sourceFormat);
                    var sourceBuffer = CreateMessageBuffer(sourceFile, sourceBufferType);
                    sourceBuffers.Add(sourceBuffer);
                }

                if (!String.IsNullOrEmpty(destinationFile)) {
                    var destinationBufferType = ParseMessageBufferType(destinationFile,
                                                                       destinationFormat);
                    destinationBuffer = CreateMessageBuffer(destinationFile,
                                                            destinationBufferType);
                    if (destinationBuffer.Count > 0) {
                        throw new InvalidOperationException(
                            String.Format(
                                _("Destination database {0} must be empty!"),
                                destinationFile
                            )
                        );
                    }
                }

                // append all messages of all source buffers together in a lazy way
                IEnumerable<MessageModel> concatenatedMessages = new List<MessageModel>(0);
                sourceBuffers.ForEach(x =>
                    concatenatedMessages = concatenatedMessages.Concat(x)
                );

                if (destinationBuffer == null) {
                    // JSON pipe
                    WriteMessagesToJson(concatenatedMessages, Console.Out);
                } else {
                    foreach (var msg in concatenatedMessages) {
                        destinationBuffer.Add(msg);
                    }
                    destinationBuffer.Flush();
                }
            } finally {
                foreach (var sourceBuffer in sourceBuffers) {
                    sourceBuffer.Dispose();
                }
                if (destinationBuffer != null) {
                    destinationBuffer.Dispose();
                }
            }
        }

        static void WriteMessagesToJson(IEnumerable<MessageModel> messages, TextWriter writer)
        {
            // OPT: if you are wondering why this code is handling the
            // serialization of JSON list manually instead of passing it as a
            // List<T> in a single method call to JsonSerializer.SerializeToWriter(dtoMessages)
            // then this is because it would mean that all messages from the
            // source message buffer would need to be read completely into
            // memory before serializing it into JSON and then writing the result
            // of that to the console or file. Instead this is a read one message
            // from the message buffer, copy it to a DTO object, serialize that
            // one message to JSON and then write that single JSON object to the
            // target which is a TextWriter.
            writer.Write("[");
            bool first = true;
            foreach (var message in messages) {
                if (first) {
                    first = false;
                } else {
                    writer.Write(",");
                }
                var dtoMessage = new MessageDtoModelV1(message);
                JsonSerializer.SerializeToWriter(dtoMessage, writer);
            }
            writer.WriteLine("]");
            writer.Flush();
        }

        static MessageBufferType ParseMessageBufferType(string fileName, string type)
        {
            if (String.IsNullOrEmpty(type)) {
                if (fileName.EndsWith(".sqlite3")) {
                    return MessageBufferType.Sqlite;
                } else if (fileName.EndsWith(".db4o")) {
                    return MessageBufferType.Db4o;
                } else {
                    throw new ArgumentException(
                        String.Format(
                            _("Unknown file format: '{0}'"),
                            fileName
                        ),
                        "fileName"
                    );
                }
            }
            return (MessageBufferType) Enum.Parse(typeof(MessageBufferType),
                                                  fileName, true);
        }

        static IMessageBuffer CreateMessageBuffer(string fileName,
                                                  MessageBufferType bufferType)
        {
            switch (bufferType) {
                case MessageBufferType.Db4o:
                    return new Db4oMessageBuffer(fileName);
                case MessageBufferType.Sqlite:
                    return new SqliteMessageBuffer(fileName);
                default:
                    throw new ArgumentException(
                        String.Format(
                            _("Unsupported buffer type: '{0}'"),
                            bufferType
                        ),
                        "bufferType"
                    );
            }
        }

        static void InitLocale()
        {
            string appDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string localeDir = Path.Combine(appDir, "locale");
            if (!Directory.Exists(localeDir)) {
                localeDir = Path.Combine(Defines.InstallPrefix, "share");
                localeDir = Path.Combine(localeDir, "locale");
            }

            LibraryCatalog.Init("smuxi-message-buffer", localeDir);
            Logger.Debug("Using locale data from: " + localeDir);
        }

        static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, LibraryTextDomain);
        }
    }

    public enum MessageBufferType {
        None,
        Pipe,
        Db4o,
        Sqlite
    }
}
