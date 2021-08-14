using System;
using Microsoft.SPOT;
using MyHome.Models;
using System.Collections;
using MyHome.Constants;
using MyHome.Extensions;
using Gadgeteer.Modules.GHIElectronics;
using MyHome.Utilities;

using GT = Gadgeteer;

namespace MyHome.Modules
{
    public class SecurityManager : ISecurityManager
    {
        private const string ConfigFilePath = Directories.Config + "\\Accounts.csv";
        private const int ScanTimeoutMs = 10000; // 10 seconds
        private readonly Logger _logger;
        private readonly IFileManager _fm;
        private readonly RFIDReader _rfid;
        private GT.Timer _scanTimer;
        private readonly Hashtable _tokens;
        private string _newRfidUsername;

        public event SecurityManager.EventHandler OnScanEnabled;

        public event SecurityManager.ScanCompleteEventHandler OnScanCompleted;

        public event SecurityManager.EventHandler OnAccessDenied;

        public event SecurityManager.AccessGrantedHandler OnAccessGranted;

        public delegate void EventHandler();

        public delegate void ScanCompleteEventHandler(bool timeoutOccurred);

        public delegate void AccessGrantedHandler(string username);

        public SecurityManager(RFIDReader rfidReader, IFileManager fileManager)
        {
            _logger = Logger.ForContext(this);
            _rfid = rfidReader;
            _rfid.IdReceived += Rfid_IdReceived;
            _rfid.MalformedIdReceived += Rfid_MalformedIdReceived;
            _fm = fileManager;
            _tokens = new Hashtable();
        }

        public void Initialise()
        {
            _logger.Information("Initialising user accounts");
            if (!_fm.FileExists(ConfigFilePath))
            {
                _fm.SaveFile(ConfigFilePath, string.Empty);
            }

            var file = _fm.GetFileString(ConfigFilePath);
            var rows = file.Split('\r', '\n');
            foreach (var line in rows)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.IsNullOrEmpty())
                {
                    continue;
                }

                var splitLine = trimmedLine.Split(',');
                if (splitLine.Length < 4) 
                {
                    _logger.Warning("Unable to parse access token from \"{0}\"", splitLine);
                    continue;
                }

                var rfid = splitLine[0].Trim();
                if (rfid.IsNullOrEmpty())
                {
                    _logger.Warning("RFID code is missing on line \"{0}\"", line);
                    continue;
                }

                var username = splitLine[1].Trim();
                if (username.IsNullOrEmpty())
                {
                    _logger.Warning("Username is missing for \"{0}\"", rfid);
                    continue;
                }

                DateTime allocated;
                if (!DateTimeParser.SortableDateTime(splitLine[2].Trim(), out allocated))
                {
                    _logger.Warning("Unable to parse allocated date from \"{0}\"", splitLine[2]);
                    continue;
                }

                var expired = DateTime.MinValue;
                var expiredStr = splitLine[3].Trim();
                if (!expiredStr.IsNullOrEmpty() &&
                    !DateTimeParser.SortableDateTime(expiredStr, out expired))
                {
                    _logger.Warning("Unable to parse expired date from \"{0}\"", expiredStr);
                    continue;
                }

                _tokens.Add(rfid, new UserAccount
                {
                    RFID = rfid,
                    Username = username,
                    Allocated = allocated,
                    Expired = expired
                });
            }

            _logger.Information("Initialised all user accounts");
        }

        public void Add(string rfid, string username)
        {
            if (rfid.IsNullOrEmpty())
            {
                throw new NullReferenceException("RFID cannot be null");
            }

            if (username.IsNullOrEmpty())
            {
                throw new NullReferenceException("Username cannot be null");
            }

            _logger.Information("Adding new access token");
            _tokens.Add(rfid, new UserAccount
            {
                RFID = rfid,
                Username = username,
                Allocated = DateTime.Now
            });
        }

        public void AddViaRfidScan(string username)
        {
            _newRfidUsername = username;

            if (_scanTimer != null && _scanTimer.IsRunning)
            {
                _scanTimer.Stop();
            }

            _scanTimer = new GT.Timer(ScanTimeoutMs);
            _scanTimer.Tick += ScanTimer_Tick;

            if (OnScanEnabled != null)
            {
                OnScanEnabled.Invoke();
            }

            _scanTimer.Start();
        }

        public void Expire(string rfid)
        {
            var token = FindByRfid(rfid);
            if (token != null)
            {
                _logger.Information("Revoking access token");
                token.Expired = DateTime.Now;
            }
        }

        public UserAccount FindByRfid(string rfid)
        {
            if (_tokens.Contains(rfid))
            {
                return (UserAccount)_tokens[rfid];
            }

            return null;
        }

        public UserAccount FindByUsername(string username)
        {
            foreach (UserAccount token in _tokens.Values)
            {
                if (token.Username == username)
                {
                    return token;
                }
            }

            return null;
        }

        public void Remove(string rfid)
        {
            if (_tokens.Contains(rfid))
            {
                _logger.Information("Removing access token");
                _tokens.Remove(rfid);
            }
        }

        private void CheckAccess(string rfid)
        {
            _logger.Information("Checking access...");
            var token = FindByRfid(rfid);

            if (token == null)
            {
                _logger.Warning("Unauthorised access attempt by \"{0}\"", rfid);
                if (OnAccessDenied != null)
                {
                    OnAccessDenied.Invoke();
                }
                return;
            }

            if (token.IsExpired)
            {
                _logger.Warning("Unauthorised access attempt by \"{0}\" ({1})", token.RFID, token.Username);
                if (OnAccessDenied != null)
                {
                    OnAccessDenied.Invoke();
                }
                return;
            }

            _logger.Information("Access granted \"{0}\"", token.Username);
            if (OnAccessGranted != null)
            {
                OnAccessGranted.Invoke(token.Username);
            }
        }

        private void Rfid_MalformedIdReceived(RFIDReader sender, EventArgs e)
        {
            _logger.Warning("Malformed RFID received...");
            if (OnAccessDenied != null)
            {
                OnAccessDenied.Invoke();
            }
        }

        private void Rfid_IdReceived(RFIDReader sender, string incomingRfid)
        {
            _logger.Information("RFID received...");
            if (!_newRfidUsername.IsNullOrEmpty())
            {
                Add(incomingRfid, _newRfidUsername);
                _newRfidUsername = null;

                if (OnScanCompleted != null)
                {
                    OnScanCompleted.Invoke(false);
                }
            }
            else
            {
                CheckAccess(incomingRfid);
            }
        }

        private void ScanTimer_Tick(GT.Timer timer)
        {
            // Clear the RFID scan if they don't scan within the time limit
            if (!_newRfidUsername.IsNullOrEmpty())
            {
                _newRfidUsername = null;

                if (OnScanCompleted != null)
                {
                    OnScanCompleted.Invoke(true);
                }
            }

            _scanTimer.Stop();
        }
    }
}
