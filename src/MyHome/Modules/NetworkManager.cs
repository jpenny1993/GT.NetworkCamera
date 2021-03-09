using System;
using System.Threading;
using Microsoft.SPOT;
using Gadgeteer.Modules.GHIElectronics;
using Gadgeteer.Networking;

using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;

namespace MyHome.Modules
{
    public enum NetworkStatus
    {
        Disabled = 0,
        Enabled = 1,
        NetworkDown = 2,
        NetworkUp = 3,
        NetworkAvailable = 4
    }

    public sealed class NetworkManager
    {
        private const int TimerTickMs = 5000; // Every 5 seconds
        private const string EmptyIpAddress = "0.0.0.0";
        private readonly EthernetJ11D _ethernet;
        private readonly GT.Timer _networkTimer;
        private bool _networkStuck;
        private NetworkStatus _prevStatus;
        private NetworkStatus _status;

        public event NetworkManager.StatusChangedEventHandler OnStatusChanged;

        public delegate void StatusChangedEventHandler(NetworkStatus status, NetworkStatus previousStatus);

        public NetworkManager(EthernetJ11D ethernetJ11D)
        {
            _ethernet = ethernetJ11D;
            _prevStatus = _status = NetworkStatus.Disabled;
            _networkTimer = new GT.Timer(TimerTickMs);
            _networkTimer.Tick += NetworkTimer_Tick;
            _networkTimer.Start();
        }

        public string IpAddress { get { return _ethernet.NetworkSettings.IPAddress; } }

        public void Disable()
        {
            _ethernet.NetworkInterface.Close();

            _ethernet.NetworkDown -= EthernetJ11D_NetworkDown;
            _ethernet.NetworkUp -= EthernetJ11D_NetworkUp;
            _status = NetworkStatus.Disabled;
        }

        public void Enable()
        {
            _ethernet.NetworkDown += EthernetJ11D_NetworkDown;
            _ethernet.NetworkUp += EthernetJ11D_NetworkUp;

            _ethernet.NetworkSettings.EnableDhcp();
            _ethernet.NetworkSettings.EnableDynamicDns();
            _ethernet.UseThisNetworkInterface();
            _status = NetworkStatus.Enabled;
            _networkStuck = false;
        }

        private void EthernetJ11D_NetworkDown(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
            Debug.Print("Automatic Network Change: Network down");
            _status = NetworkStatus.NetworkDown;
            _networkStuck = false;
        }

        private void EthernetJ11D_NetworkUp(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
            Debug.Print("Automatic Network Change: Network up");
            _status = NetworkStatus.NetworkUp;
        }

        private void NetworkTimer_Tick(GT.Timer timer)
        {
            // Manually handle network state
            var networkStatus = _status;
            switch (networkStatus)
            {
                default: break;
                case NetworkStatus.Enabled:
                    if (_ethernet.IsNetworkConnected)
                    {
                        Debug.Print("Manual Network Change: Cable connected");
                        networkStatus = NetworkStatus.NetworkDown;
                    }
                    break;
                case NetworkStatus.NetworkDown:
                    if (_ethernet.IsNetworkUp)
                    {
                        Debug.Print("Manual Network Change: Network up");
                        networkStatus = NetworkStatus.NetworkUp;
                    }
                    else if (!_ethernet.IsNetworkConnected)
                    {
                        Debug.Print("Manual Network Change: Cable disconnected");
                        networkStatus = NetworkStatus.Enabled;
                    }
                    else if (!_networkStuck)
                    {
                        _networkStuck = true;
                        Debug.Print("Network Startup Stuck, unplug cable and reinsert network cable");
                    }
                    break;
                case NetworkStatus.NetworkUp:
                    if (_networkStuck)
                    {
                        _networkStuck = false;
                    }

                    if (_ethernet.NetworkSettings.IPAddress != EmptyIpAddress)
                    {
                        Debug.Print("Manual Network Change: Network available");
                        networkStatus = NetworkStatus.NetworkAvailable;
                    }
                    break;
            }

            if (networkStatus != _status) 
            {
                _status = networkStatus;
            }

            // Notify subscribers
            if (networkStatus != _prevStatus)
            {
                if (OnStatusChanged != null)
                {
                    OnStatusChanged.Invoke(_status, _prevStatus);
                }

                _prevStatus = networkStatus;
            }
        }
    }
}
