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
        NetworkStuck = 2,
        NetworkDown = 3,
        NetworkUp = 4,
        NetworkAvailable = 5
    }

    public sealed class NetworkManager
    {
        private const int TimerTickMs = 5000; // Every 5 seconds
        private const string EmptyIpAddress = "0.0.0.0";
        private const string DefaultSubnet = "255.255.255.0";
        private readonly Logger _logger;
        private readonly EthernetJ11D _ethernet;
        private readonly GT.Timer _networkTimer;
        private NetworkStatus _prevStatus;
        private NetworkStatus _status;

        public event NetworkManager.StatusChangedEventHandler OnStatusChanged;

        public delegate void StatusChangedEventHandler(NetworkStatus status, NetworkStatus previousStatus);

        public NetworkManager(EthernetJ11D ethernetJ11D)
        {
            _logger = Logger.ForContext(this);
            _ethernet = ethernetJ11D;
            _prevStatus = _status = NetworkStatus.Disabled;
            _networkTimer = new GT.Timer(TimerTickMs);
            _networkTimer.Tick += NetworkTimer_Tick;
            _networkTimer.Start();
        }

        public string IpAddress { get { return _ethernet.NetworkSettings.IPAddress; } }

        public void ModeDhcp()
        {
            _logger.Information("Enabling DHCP");
            _ethernet.NetworkSettings.EnableDhcp();
        }

        public void ModeStatic(string ipAddress, string subnet = DefaultSubnet, string gateway = EmptyIpAddress)
        {
            _logger.Information("Enabling Static IP");
            _ethernet.NetworkSettings.EnableStaticIP(ipAddress, subnet, gateway);
        }

        public void Disable()
        {
            _logger.Information("Disabling network interface");
            _ethernet.NetworkInterface.Close();

            _ethernet.NetworkDown -= EthernetJ11D_NetworkDown;
            _ethernet.NetworkUp -= EthernetJ11D_NetworkUp;
            _status = NetworkStatus.Disabled;
        }

        public void Enable()
        {
            _logger.Information("Enabling network interface");
            _ethernet.NetworkDown += EthernetJ11D_NetworkDown;
            _ethernet.NetworkUp += EthernetJ11D_NetworkUp;

            _ethernet.NetworkSettings.EnableDynamicDns();
            _ethernet.UseThisNetworkInterface();
            _status = NetworkStatus.Enabled;
        }

        private void EthernetJ11D_NetworkDown(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
            _logger.Information("Auto - Network down");
            _status = NetworkStatus.NetworkDown;
        }

        private void EthernetJ11D_NetworkUp(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
            _logger.Information("Auto - Network up");
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
                        _logger.Information("Manual - Cable connected");
                        networkStatus = NetworkStatus.NetworkDown;
                    }
                    break;
                case NetworkStatus.NetworkDown:
                    if (_ethernet.IsNetworkUp)
                    {
                        _logger.Information("Manual - Network up");
                        networkStatus = NetworkStatus.NetworkUp;
                    }
                    else if (!_ethernet.IsNetworkConnected)
                    {
                        _logger.Information("Manual - Cable disconnected");
                        networkStatus = NetworkStatus.Enabled;
                    }
                    else
                    {
                        networkStatus = NetworkStatus.NetworkStuck;
                        _logger.Information("Network Startup Stuck - unplug cable and reinsert network cable");
                    }
                    break;
                case NetworkStatus.NetworkUp:
                    if (_ethernet.NetworkSettings.IPAddress != EmptyIpAddress)
                    {
                        _logger.Information("Manual - Network available");
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
