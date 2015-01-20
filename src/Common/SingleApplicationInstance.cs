// This file is part of Smuxi and is licensed under the terms of MIT/X11
//
// Copyright (c) 2015 Mirco Bauer <meebey@meebey.net>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Threading;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.IO;

#if MONO_UNIX
using Mono.Unix;
using Mono.Unix.Native;
#endif

namespace Smuxi.Common
{
    public class SingleApplicationInstance<T> : IDisposable where T : MarshalByRefObject
    {
        public string Identifier { get; private set; }
        public bool IsFirstInstance { get; private set; }
        Mutex FirstInstanceMutex { get; set; }
#if MONO_UNIX
        UnixFileInfo FirstInstanceFileInfo { get; set; }
        UnixStream FirstInstanceFileStream { get; set; }
        Thread UnixSignalThread { get; set; }
#endif
        IChannel RemotingChannel { get; set; }
        string RemotingObjectName { get; set; }

        T f_FirstInstance;
        public T FirstInstance {
            get {
                if (f_FirstInstance == default(T)) {
                    if (IsFirstInstance) {
                        throw new InvalidOperationException("FirstInstance must be initialized first.");
                    } else {
                        ConnectToFirstInstance();
                    }
                }
                return f_FirstInstance;
            }
            set {
                if (value == null) {
                    throw new ArgumentNullException("value");
                }
                if (!IsFirstInstance) {
                    throw new InvalidOperationException("FirstInstance must be initialized by the first instance.");
                }

                f_FirstInstance = value;
                ExposeFirstInstance();
            }
        }

        public SingleApplicationInstance()
        {
            Identifier = typeof(T).Assembly.Location;
            // Mono's IPC does not like \ or / in the name
            // On MS .NET Local\ as a special and valid prefix!
            Identifier = Identifier.Replace("\\", "_").Replace("/", "_");
            Init();
        }

        public SingleApplicationInstance(string instanceIdentifier)
        {
            if (instanceIdentifier == null) {
                throw new ArgumentNullException("instanceIdentifier");
            }
            Identifier = instanceIdentifier;
            Init();
        }

        ~SingleApplicationInstance()
        {
            Dispose(false);
        }

        void Init()
        {
            RemotingObjectName = "SingleApplicationInstance";

            // MS .NET on Windows -> named mutex
            // Mono on Windows -> named mutex
            // Mono w/ SHM on Linux -> named mutex
            // Mono w/o SHM on Linux -> file lock
            var platform = Environment.OSVersion.Platform;
            switch (platform) {
                case PlatformID.Win32NT:
                    InitMutex();
                    break;
                case PlatformID.Unix:
                    var has_shm = false;
                    if (IsRunningOnMono()) {
                        // we can only assume that named mutex are available if 
                        // MONO_ENABLE_SHM is set and MONO_DISABLE_SHM is unset
                        var enable_shm = Environment.GetEnvironmentVariable("MONO_ENABLE_SHM");
                        var disalbe_shm = Environment.GetEnvironmentVariable("MONO_DISABLE_SHM");
                        has_shm = !String.IsNullOrEmpty(enable_shm) &&
                                  String.IsNullOrEmpty(disalbe_shm);
                    }

                    if (has_shm) {
                        InitMutex();
                    } else {
                        InitFileLock();
                    }
                    break;
                default:
                    throw new NotSupportedException(
                        String.Format(
                            "Unknown/unsupported operating system: {0}",
                            platform
                        )
                    );
            }
        }

        void InitMutex()
        {
            bool isFirstInstance;
            FirstInstanceMutex = new Mutex(true, Identifier, out isFirstInstance);
            IsFirstInstance = isFirstInstance;
        }

#if MONO_UNIX
        string GetFileLockDirectory()
        {
            string lockDirRoot = null;

            var xdg_runtime_dir = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
            if (!String.IsNullOrEmpty(xdg_runtime_dir)) {
                // XDG_RUNTIME_DIR (/run/user/$UID) is the preferred location as it
                // gets purged every reboot. Thus stalled file locks would
                // automatically go away after a reboot.
                lockDirRoot = xdg_runtime_dir;
            } else {
                // XDG_CACHE_HOME or ~/.cache is a good fallback if XDG_RUNTIME_DIR
                // is not available as other users can't write there.
                var xdg_cache_home = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
                if (String.IsNullOrEmpty(xdg_cache_home)) {
                    var home = Environment.GetEnvironmentVariable("HOME");
                    lockDirRoot = Path.Combine(home, ".cache");
                } else {
                    lockDirRoot = xdg_cache_home;
                }
            }
            // /tmp or /dev/shm? No, thanks! We can't use those as rendezvous point
            // as other users could easy break it.

            return Path.Combine(lockDirRoot, "SingleApplicationInstance");
        }

        void InitFileLock()
        {
            var lockDirectory = GetFileLockDirectory();
            if (!Directory.Exists(lockDirectory)) {
                Directory.CreateDirectory(lockDirectory);
            }
            var lockFile = Path.Combine(lockDirectory, Identifier);
            FirstInstanceFileInfo = new UnixFileInfo(lockFile);
            try {
                FirstInstanceFileStream = FirstInstanceFileInfo.Open(
                    OpenFlags.O_CREAT | OpenFlags.O_EXCL,
                    FilePermissions.S_IRWXU
                );
                IsFirstInstance = true;
            } catch (Exception) {
                IsFirstInstance = false;
            }

            if (IsFirstInstance) {
                // managed shutdown
                AppDomain.CurrentDomain.ProcessExit += (sender, e) => {
                    ReleaseFileLock();
                };

                // signal handlers
                UnixSignal[] shutdown_signals = {
                    new UnixSignal(Signum.SIGINT),
                    new UnixSignal(Signum.SIGTERM),
                };
                UnixSignalThread = new Thread(() => {
                    UnixSignal.WaitAny(shutdown_signals);
                    ReleaseFileLock();
                });
                UnixSignalThread.Start();
            }
        }

        // this method is idempotent
        void ReleaseFileLock()
        {
            var lockFileInfo = FirstInstanceFileInfo;
            if (lockFileInfo == null) {
                return;
            }
            if (!IsFirstInstance) {
                return;
            }

            FirstInstanceFileInfo = null;
            if (!lockFileInfo.Exists) {
                return;
            }
            lockFileInfo.Delete();
        }
#else
        void InitFileLock()
        {
            throw new NotSupportedException("SingleApplicationInstance was built without MONO_UNIX support.");
        }
#endif

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            var channel = RemotingChannel;
            if (channel != null) {
                RemotingChannel = null;
                ChannelServices.UnregisterChannel(channel);
            }

            var mutex = FirstInstanceMutex;
            if (mutex != null) {
                FirstInstanceMutex = null;
                if (IsFirstInstance && disposing) {
                    // HACK: we are not allowed to release the mutex from a
                    // thread that hasn't created it! Thus we only release
                    // if Dispose() was called and not from the finalizer.
                    mutex.ReleaseMutex();
                }
                mutex.Close();
                mutex.Dispose();
            }

#if MONO_UNIX
            var lockStream = FirstInstanceFileStream;
            if (lockStream != null) {
                FirstInstanceFileStream = null;
                lockStream.Close();
            }
            ReleaseFileLock();
            var signalThread = UnixSignalThread;
            if (signalThread != null) {
                UnixSignalThread = null;
                try {
                    signalThread.Abort();
                } catch {
                }
            }
#endif
        }

        void ExposeFirstInstance()
        {
            RemotingServices.Marshal(FirstInstance, RemotingObjectName);
            RemotingChannel = new IpcChannel(Identifier);
            ChannelServices.RegisterChannel(RemotingChannel, false);
        }

        void ConnectToFirstInstance()
        {
            RemotingChannel = new IpcClientChannel();
            ChannelServices.RegisterChannel(RemotingChannel, false);
            f_FirstInstance = (T) Activator.GetObject(typeof(T), "ipc://" + Identifier + "/" + RemotingObjectName);
        }

        static bool IsRunningOnMono()
        {
            return Type.GetType("Mono.Runtime") != null;
        }
    }
}
