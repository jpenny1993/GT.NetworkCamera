﻿using System;
namespace MyHome.Modules
{
    public interface INetworkManager
    {
        void Disable();
        void Enable();
        string IpAddress { get; }
        void ModeDhcp();
        void ModeStatic(string ipAddress, string subnet, string gateway);
        void UpdateNetworkStatus();
        event NetworkManager.StatusChangedEventHandler OnStatusChanged;
    }
}
