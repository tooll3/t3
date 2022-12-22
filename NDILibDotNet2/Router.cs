using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NewTek.NDI
{
    public class Router : IDisposable, INotifyPropertyChanged
    {
        [Category("NewTek NDI"),
        Description("The NDI source to route elsewhere. An empty new Source() or a Source with no Name will disconnect.")]
        public Source SelectedSource
        {
            get { return _selectedSource; }
            set
            {
                if (value.Name != _selectedSource.Name)
                {
                    _selectedSource = value;

                    UpdateRouting();

                    NotifyPropertyChanged("FromSource");
                }
            }
        }

        [Category("NewTek NDI"),
        Description("The name that will be given to the routed source. If empty it will default to 'Routing'.")]
        public String RoutingName
        {
            get { return String.IsNullOrWhiteSpace(_routingName) ? "Routing" : _routingName; }
            set
            {
                if (value != _routingName)
                {
                    _routingName = String.IsNullOrWhiteSpace(value) ? "Routing" : value;

                    // start over if the routing name changes
                    CreateRouting();

                    NotifyPropertyChanged("RoutingName");
                }
            }
        }

        // Constructor
        public Router(String routingName = "Routing", String[] groups = null)
        {
            _groups = groups;
            _routingName = routingName;

            CreateRouting();
        }

        // Route to nowhere (black)
        public void Clear()
        {
            if (_routingInstancePtr != IntPtr.Zero)
                NDIlib.routing_clear(_routingInstancePtr);
        }

        // This will reenable routing if previous cleared.
        // Should not be needed otherwise since FromSource changes will automatically update.
        public void UpdateRouting()
        {
            // never started before?
            if (_routingInstancePtr == IntPtr.Zero)
            {
                CreateRouting();
                return;
            }

            // Sanity
            if (_selectedSource == null || String.IsNullOrEmpty(_selectedSource.Name))
            {
                Clear();
                return;
            }

            // a source_t to describe the source to connect to.
            NDIlib.source_t source_t = new NDIlib.source_t()
            {
                p_ndi_name = UTF.StringToUtf8(_selectedSource.Name)
            };

            if (!NDIlib.routing_change(_routingInstancePtr, ref source_t))
            {
                // free the memory we allocated with StringToUtf8
                Marshal.FreeHGlobal(source_t.p_ndi_name);

                throw new InvalidOperationException("Failed to change routing.");
            }

            // free the memory we allocated with StringToUtf8
            Marshal.FreeHGlobal(source_t.p_ndi_name);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Router()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Clear();
                }

                if (_routingInstancePtr != IntPtr.Zero)
                {
                    NDIlib.routing_destroy(_routingInstancePtr);
                    _routingInstancePtr = IntPtr.Zero;
                }

                _disposed = true;
            }
        }

        private bool _disposed = false;

        private void CreateRouting()
        {
            if (_routingInstancePtr != IntPtr.Zero)
            {
                NDIlib.routing_destroy(_routingInstancePtr);
                _routingInstancePtr = IntPtr.Zero;
            }

            // Sanity check
            if (_selectedSource == null || String.IsNullOrEmpty(_selectedSource.Name))
                return;

            // .Net interop doesn't handle UTF-8 strings, so do it manually
            // These must be freed later
            IntPtr sourceNamePtr = UTF.StringToUtf8(_routingName);

            IntPtr groupsNamePtr = IntPtr.Zero;

            // make a flat list of groups if needed
            if (_groups != null)
            {
                StringBuilder flatGroups = new StringBuilder();
                foreach (String group in _groups)
                {
                    flatGroups.Append(group);
                    if (group != _groups.Last())
                    {
                        flatGroups.Append(',');
                    }
                }

                groupsNamePtr = UTF.StringToUtf8(flatGroups.ToString());
            }

            // Create an NDI routing description
            NDIlib.routing_create_t createDesc = new NDIlib.routing_create_t()
            {
                p_ndi_name = sourceNamePtr,
                p_groups = groupsNamePtr
            };

            // create the NDI routing instance
            _routingInstancePtr = NDIlib.routing_create(ref createDesc);

            // free the strings we allocated
            Marshal.FreeHGlobal(sourceNamePtr);
            Marshal.FreeHGlobal(groupsNamePtr);

            // did it succeed?
            if (_routingInstancePtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to create routing instance.");
            }

            // update in case we have enough info to start routing
            UpdateRouting();
        }

        private String[] _groups = null;
        private IntPtr _routingInstancePtr = IntPtr.Zero;
        private Source _selectedSource = new Source();
        private String _routingName = "Routing";
    }
}
