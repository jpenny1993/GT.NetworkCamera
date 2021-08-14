using System;

namespace MyHome.Modules
{
    public interface ISecurityManager
    {
        event SecurityManager.EventHandler OnAccessDenied;
        event SecurityManager.AccessGrantedHandler OnAccessGranted;
        event SecurityManager.ScanCompleteEventHandler OnScanCompleted;
        event SecurityManager.EventHandler OnScanEnabled;

        void Add(string rfid, string username);
        void AddViaRfidScan(string username);
        void Expire(string rfid);
        MyHome.Models.UserAccount FindByRfid(string rfid);
        MyHome.Models.UserAccount FindByUsername(string username);
        void Initialise();
        void Remove(string rfid);
    }
}
