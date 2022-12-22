using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Data;

namespace NewTek.NDI
{
    public class Finder : IDisposable
    {
        public ObservableCollection<Source> Sources
        {
            get { return _sourceList; }
        }

        public Finder(bool showLocalSources = false, String[] groups = null, String[] extraIps = null)
        {
            BindingOperations.EnableCollectionSynchronization(_sourceList, _sourceLock);

            IntPtr groupsNamePtr = IntPtr.Zero;

            // make a flat list of groups if needed
            if (groups != null)
            {
                StringBuilder flatGroups = new StringBuilder();
                foreach (String group in groups)
                {
                    flatGroups.Append(group);
                    if (group != groups.Last())
                    {
                        flatGroups.Append(',');
                    }
                }

                groupsNamePtr = UTF.StringToUtf8(flatGroups.ToString());
            }

            // This is also optional.
            // The list of additional IP addresses that exist that we should query for 
            // sources on. For instance, if you want to find the sources on a remote machine
            // that is not on your local sub-net then you can put a comma seperated list of 
            // those IP addresses here and those sources will be available locally even though
            // they are not mDNS discoverable. An example might be "12.0.0.8,13.0.12.8".
            // When none is specified (IntPtr.Zero) the registry is used.
            // Create a UTF-8 buffer from our string
            // Must use Marshal.FreeHGlobal() after use!
            // IntPtr extraIpsPtr = NDI.Common.StringToUtf8("12.0.0.8,13.0.12.8")
            IntPtr extraIpsPtr = IntPtr.Zero;

            // make a flat list of ip addresses as comma separated strings
            if (extraIps != null)
            {
                StringBuilder flatIps = new StringBuilder();
                foreach (String ipStr in extraIps)
                {
                    flatIps.Append(ipStr);
                    if (ipStr != groups.Last())
                    {
                        flatIps.Append(',');
                    }
                }

                extraIpsPtr = UTF.StringToUtf8(flatIps.ToString());
            }

            // how we want our find to operate
            NDIlib.find_create_t findDesc = new NDIlib.find_create_t()
            {
                p_groups = groupsNamePtr,
                show_local_sources = showLocalSources,
                p_extra_ips = extraIpsPtr

            };

            // create our find instance
            _findInstancePtr = NDIlib.find_create_v2(ref findDesc);

            // free our UTF-8 buffer if we created one
            if (groupsNamePtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(groupsNamePtr);
            }

            if (extraIpsPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(extraIpsPtr);
            }

            // start up a thread to update on
            _findThread = new Thread(FindThreadProc) { IsBackground = true, Name = "NdiFindThread" };
            _findThread.Start();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Finder()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // tell the thread to exit
                    _exitThread = true;

                    // wait for it to exit
                    if (_findThread != null)
                    {
                        _findThread.Join();

                        _findThread = null;
                    }
                }

                if (_findInstancePtr != IntPtr.Zero)
                {
                    NDIlib.find_destroy(_findInstancePtr);
                    _findInstancePtr = IntPtr.Zero;
                }

                _disposed = true;
            }
        }

        private bool _disposed = false;

        private void FindThreadProc()
        {
            // the size of an NDIlib.source_t, for pointer offsets
            int SourceSizeInBytes = Marshal.SizeOf(typeof(NDIlib.source_t));

            while (!_exitThread)
            {
                // Wait up to 500ms sources to change
                if (NDIlib.find_wait_for_sources(_findInstancePtr, 500))
                {
                    uint NumSources = 0;
                    IntPtr SourcesPtr = NDIlib.find_get_current_sources(_findInstancePtr, ref NumSources);

                    // convert each unmanaged ptr into a managed NDIlib.source_t
                    for (int i = 0; i < NumSources; i++)
                    {
                        // source ptr + (index * size of a source)
                        IntPtr p = IntPtr.Add(SourcesPtr, (i * SourceSizeInBytes));

                        // marshal it to a managed source and assign to our list
                        NDIlib.source_t src = (NDIlib.source_t)Marshal.PtrToStructure(p, typeof(NDIlib.source_t));

                        // .Net doesn't handle marshaling UTF-8 strings properly
                        String name = UTF.Utf8ToString(src.p_ndi_name);

                        // Add it to the list if not already in the list.
                        // We don't have to remove because NDI applications remember any sources seen during each run.
                        // They might be selected and come back when the connection is restored.
                        if (!_sourceList.Any(item => item.Name == name))
                        {
                            _sourceList.Add(new Source(src));
                        }
                    }
                }
            }
        }

        private IntPtr _findInstancePtr = IntPtr.Zero;

        private ObservableCollection<Source> _sourceList = new ObservableCollection<Source>();
        private object _sourceLock = new object();

        // a thread to find on so that the UI isn't dragged down
        Thread _findThread = null;

        // a way to exit the thread safely
        bool _exitThread = false;
    }
}
