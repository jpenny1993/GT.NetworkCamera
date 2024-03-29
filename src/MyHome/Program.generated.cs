//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MyHome {
    using Gadgeteer;
    using GTM = Gadgeteer.Modules;
    
    
    public partial class Program : Gadgeteer.Program {
        
        /// <summary>The Camera module using socket 3 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.Camera camera;
        
        /// <summary>The USB Client DP module using socket 1 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.USBClientDP usbClientDP;
        
        /// <summary>The Ethernet J11D module using socket 7 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.EthernetJ11D ethernetJ11D;
        
        /// <summary>The Button module using socket 11 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.Button button;
        
        /// <summary>The Multicolor LED module using socket 6 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.MulticolorLED infoLED;
        
        /// <summary>The SD Card module using socket 5 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.SDCard sdCard;
        
        /// <summary>The Temp&Humidity module using socket 4 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.TempHumidity tempHumidity;
        
        /// <summary>The LightSense module using socket 9 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.LightSense lightSense;
        
        /// <summary>The RFID Reader module using socket 8 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.RFIDReader rfidReader;
        
        /// <summary>The Display T35 module using sockets 14, 13, 12 and 10 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.DisplayT35 displayT35;
        
        /// <summary>The Multicolor LED module using socket * of infoLED.</summary>
        private Gadgeteer.Modules.GHIElectronics.MulticolorLED networkLED;
        
        /// <summary>This property provides access to the Mainboard API. This is normally not necessary for an end user program.</summary>
        protected new static GHIElectronics.Gadgeteer.FEZSpider Mainboard {
            get {
                return ((GHIElectronics.Gadgeteer.FEZSpider)(Gadgeteer.Program.Mainboard));
            }
            set {
                Gadgeteer.Program.Mainboard = value;
            }
        }
        
        /// <summary>This method runs automatically when the device is powered, and calls ProgramStarted.</summary>
        public static void Main() {
            // Important to initialize the Mainboard first
            Program.Mainboard = new GHIElectronics.Gadgeteer.FEZSpider();
            Program p = new Program();
            p.InitializeModules();
            p.ProgramStarted();
            // Starts Dispatcher
            p.Run();
        }
        
        private void InitializeModules() {
            this.camera = new GTM.GHIElectronics.Camera(3);
            this.usbClientDP = new GTM.GHIElectronics.USBClientDP(1);
            this.ethernetJ11D = new GTM.GHIElectronics.EthernetJ11D(7);
            this.button = new GTM.GHIElectronics.Button(11);
            this.infoLED = new GTM.GHIElectronics.MulticolorLED(6);
            this.sdCard = new GTM.GHIElectronics.SDCard(5);
            this.tempHumidity = new GTM.GHIElectronics.TempHumidity(4);
            this.lightSense = new GTM.GHIElectronics.LightSense(9);
            this.rfidReader = new GTM.GHIElectronics.RFIDReader(8);
            this.displayT35 = new GTM.GHIElectronics.DisplayT35(14, 13, 12, 10);
            this.networkLED = new GTM.GHIElectronics.MulticolorLED(this.infoLED.DaisyLinkSocketNumber);
        }
    }
}
