using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace NewTek.NDI
{
    public class Sender : IDisposable
    {
        public Sender(String sourceName, bool clockVideo = true, bool clockAudio = false, String[] groups = null, String failoverName = null)
        {
            if (String.IsNullOrEmpty(sourceName))
            {
                throw new ArgumentException("sourceName can not be null or empty.", sourceName);
            }

            // .Net interop doesn't handle UTF-8 strings, so do it manually
            // These must be freed later
            IntPtr sourceNamePtr = UTF.StringToUtf8(sourceName);

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

            // Create an NDI source description
            NDIlib.send_create_t createDesc = new NDIlib.send_create_t()
            {
                p_ndi_name = sourceNamePtr,
                p_groups = groupsNamePtr,
                clock_video = clockVideo,
                clock_audio = clockAudio
            };

            // create the NDI send instance
            _sendInstancePtr = NDIlib.send_create(ref createDesc);

            // free the strings we allocated
            Marshal.FreeHGlobal(sourceNamePtr);
            Marshal.FreeHGlobal(groupsNamePtr);

            // did it succeed?
            if (_sendInstancePtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to create send instance.");
            }

            if (!String.IsNullOrEmpty(failoverName))
            {
                // .Net interop doesn't handle UTF-8 strings, so do it manually
                // These must be freed later
                IntPtr failoverNamePtr = UTF.StringToUtf8(failoverName);

                NDIlib.source_t failoverDesc = new NDIlib.source_t()
                {
                    p_ndi_name = failoverNamePtr,
                    p_url_address = IntPtr.Zero
                };

                NDIlib.send_set_failover(_sendInstancePtr, ref failoverDesc);

                // free the strings we allocated
                Marshal.FreeHGlobal(failoverNamePtr);
            }
        }

        // The current tally state
        public NDIlib.tally_t Tally
        {
            get
            {
                if (_sendInstancePtr == IntPtr.Zero)
                    return _ndiTally;

                NDIlib.send_get_tally(_sendInstancePtr, ref _ndiTally, 0);

                return _ndiTally;
            }
        }

        // Determine the current tally sate. If you specify a timeout then it will wait until it has changed, otherwise it will simply poll it
        // and return the current tally immediately. The return value is whether anything has actually change (true) or whether it timed out (false)
        public bool GetTally(ref NDIlib.tally_t tally, int timeout)
        {
            if (_sendInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.send_get_tally(_sendInstancePtr, ref tally, (uint)timeout);
        }

        // The number of current connections
        public int Connections
        {
            get
            {
                if (_sendInstancePtr == IntPtr.Zero)
                    return 0;

                return NDIlib.send_get_no_connections(_sendInstancePtr, 0);
            }
        }

        // Get the current number of receivers connected to this source. This can be used to avoid even rendering when nothing is connected to the video source.
        // which can significantly improve the efficiency if you want to make a lot of sources available on the network. If you specify a timeout that is not
        // 0 then it will wait until there are connections for this amount of time.
        public int GetConnections(int waitMs)
        {
            if (_sendInstancePtr == IntPtr.Zero)
                return 0;

            return NDIlib.send_get_no_connections(_sendInstancePtr, (uint)waitMs);
        }

        public void Send(VideoFrame videoFrame)
        {
            Send(ref videoFrame._ndiVideoFrame);
        }

        public void Send(ref NDIlib.video_frame_v2_t videoFrame)
        {
            if (_sendInstancePtr == IntPtr.Zero)
                return;

            NDIlib.send_send_video_v2(_sendInstancePtr, ref videoFrame);
        }

        public void Send(AudioFrame audioFrame)
        {
            Send(ref audioFrame._ndiAudioFrame);
        }

        public void Send(ref NDIlib.audio_frame_v2_t audioFrame)
        {
            if (_sendInstancePtr == IntPtr.Zero)
                return;

            NDIlib.send_send_audio_v2(_sendInstancePtr, ref audioFrame);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Sender()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_sendInstancePtr != IntPtr.Zero)
                {
                    NDIlib.send_destroy(_sendInstancePtr);
                    _sendInstancePtr = IntPtr.Zero;
                }
            }
        }

        private IntPtr _sendInstancePtr = IntPtr.Zero;
        private NDIlib.tally_t _ndiTally = new NDIlib.tally_t();
    }
}
