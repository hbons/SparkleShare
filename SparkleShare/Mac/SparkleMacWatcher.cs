//   Originally taken from:
//   https://github.com/jesse99/Continuum/blob/master/source/shared/DirectoryWatcher.cs
//   Modified to use MonoMac and integrate into SparkleShare
//
//   Copyright (C) 2008 Jesse Jones
//   Copyright (C) 2012 Hylke Bons
//
//   Permission is hereby granted, free of charge, to any person obtaining
//   a copy of this software and associated documentation files (the
//   "Software"), to deal in the Software without restriction, including
//   without limitation the rights to use, copy, modify, merge, publish,
//   distribute, sublicense, and/or sell copies of the Software, and to
//   permit persons to whom the Software is furnished to do so, subject to
//   the following conditions:
//
//   The above copyright notice and this permission notice shall be
//   included in all copies or substantial portions of the Software.
//
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//   EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//   MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//   NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//   LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
//   OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
//   WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using System.Timers;

using MonoMac.AppKit;
using MonoMac.Foundation;

namespace SparkleShare {

    public sealed class SparkleMacWatcher : IDisposable {
        
        public delegate void ChangedEventHandler (string path);
        public event ChangedEventHandler Changed;

        public string Path { get; private set; }


        [Flags]
        [Serializable]
        private enum FSEventStreamCreateFlags : uint
        {
            kFSEventStreamCreateFlagNone       = 0x00000000,
            kFSEventStreamCreateFlagUseCFTypes = 0x00000001,
            kFSEventStreamCreateFlagNoDefer    = 0x00000002,
            kFSEventStreamCreateFlagWatchRoot  = 0x00000004,
        }

        private DateTime last_found_timestamp;
        private IntPtr m_stream;
        private FSEventStreamCallback m_callback; // need to keep a reference around so that it isn't GC'ed
        private static readonly IntPtr kCFRunLoopDefaultMode = new NSString ("kCFRunLoopDefaultMode").Handle;
        private ulong kFSEventStreamEventIdSinceNow          = 0xFFFFFFFFFFFFFFFFUL;

        private delegate void FSEventStreamCallback (IntPtr streamRef, IntPtr clientCallBackInfo,
            int numEvents, IntPtr eventPaths, IntPtr eventFlags, IntPtr eventIds);


        ~SparkleMacWatcher ()
        {
            Dispose (false);
        }


        public SparkleMacWatcher (string path)
        {
            Path       = path;
            m_callback = DoCallback;

            NSString [] s  = new NSString [1];
            s [0]          = new NSString (path);
            NSArray path_p = NSArray.FromNSObjects (s);

            m_stream = FSEventStreamCreate ( // note that the stream will always be valid
                IntPtr.Zero, // allocator
                m_callback, // callback
                IntPtr.Zero, // context
                path_p.Handle, // pathsToWatch
                kFSEventStreamEventIdSinceNow, // sinceWhen
                2, // latency (in seconds)
                FSEventStreamCreateFlags.kFSEventStreamCreateFlagNone); // flags

            FSEventStreamScheduleWithRunLoop (
                m_stream, // streamRef
                CFRunLoopGetMain(), // runLoop
                kCFRunLoopDefaultMode); // runLoopMode

            bool started = FSEventStreamStart (m_stream);
            if (!started) {
                GC.SuppressFinalize (this);
                throw new InvalidOperationException ("Failed to start FSEvent stream for " + path);
            }
        }


        public void Dispose ()
        {
            Dispose (true);
            GC.SuppressFinalize (this);
        }


        private void Dispose (bool disposing)
        {
            if (m_stream != IntPtr.Zero) {
                FSEventStreamStop (m_stream);
                FSEventStreamInvalidate (m_stream);
                FSEventStreamRelease (m_stream);

                m_stream = IntPtr.Zero;
            }
        }


        private void checkDirectory (string dir)
        {
            if (dir == null)
                return;

            DirectoryInfo parent = new DirectoryInfo (dir);

            if (!parent.FullName.Contains ("/.") &&
                DateTime.Compare (parent.LastWriteTime, this.last_found_timestamp) > 0) {

                last_found_timestamp = parent.LastWriteTime;
            }
        }


        private void DoCallback (IntPtr streamRef, IntPtr clientCallBackInfo,
            int numEvents, IntPtr eventPaths, IntPtr eventFlags, IntPtr eventIds)
        {
            int bytes = Marshal.SizeOf (typeof (IntPtr));
            string [] paths = new string [numEvents];

            for (int i = 0; i < numEvents; ++i) {
                IntPtr p = Marshal.ReadIntPtr (eventPaths, i * bytes);
                paths [i] = Marshal.PtrToStringAnsi (p);
                checkDirectory (paths [i]);
            }

            var handler = Changed;
            if (handler != null) {
                if (paths [0].Length >= Path.Length) {
                    string path = paths [0];
                    path = path.Substring (Path.Length);
                    path = path.Trim ("/".ToCharArray ());

                    if (!string.IsNullOrWhiteSpace (path))
                        handler (path);
                }
            }

            GC.KeepAlive (this);
        }


        [DllImport("/System/Library/Frameworks/CoreServices.framework/CoreServices")]
        private extern static IntPtr CFRunLoopGetMain ();
      
        [DllImport("/System/Library/Frameworks/CoreServices.framework/CoreServices")]
        private extern static IntPtr FSEventStreamCreate (IntPtr allocator, FSEventStreamCallback callback,
            IntPtr context, IntPtr pathsToWatch, ulong sinceWhen, double latency, FSEventStreamCreateFlags flags);

        [DllImport("/System/Library/Frameworks/CoreServices.framework/CoreServices")]
        private extern static void FSEventStreamScheduleWithRunLoop (IntPtr streamRef, IntPtr runLoop, IntPtr runLoopMode);

        [return: MarshalAs (UnmanagedType.U1)]
        [DllImport("/System/Library/Frameworks/CoreServices.framework/CoreServices")]
        private extern static bool FSEventStreamStart (IntPtr streamRef);

        [DllImport("/System/Library/Frameworks/CoreServices.framework/CoreServices")]
        private extern static void FSEventStreamStop (IntPtr streamRef);

        [DllImport("/System/Library/Frameworks/CoreServices.framework/CoreServices")]
        private extern static void FSEventStreamInvalidate (IntPtr streamRef);

        [DllImport("/System/Library/Frameworks/CoreServices.framework/CoreServices")]
        private extern static void FSEventStreamRelease (IntPtr streamRef);
    }
}
