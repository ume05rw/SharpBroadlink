SharpBroadlink - for Broadlink RM Pro IR Controller
====

A simple C# API for controlling RM Pro from [Broadlink](http://www.ibroadlink.com/rm/).  
This is a port of [python-broadlink](https://github.com/mjg59/python-broadlink) to C# .Net Standard.  

## Description

Supported device is RM Pro only.  
The original python-broadlink supports A1 sensors and more, but I have not implemented it yet.  
I don't have these devices.  

Supports .NET Standard2.0

## Requirement
[Xb.Core](https://www.nuget.org/packages/Xb.Core/)  
[Xb.Net](https://github.com/ume05rw/Xb.Net/)  

## Usage
1. Create your new C# project.
2. Download this solution.
3. Add a project-reference SharpBroadlink/SharpBroadlink.csproj to your project.
4. Setup devices or discover devices as follows:


ex) Setup:  

== Preparation - It is the [same as the original](https://github.com/mjg59/python-broadlink). ==  
1. Put the device into AP Mode.  
2. Long press the reset button until the blue LED is blinking quickly.  
3. Long press again until blue LED is blinking slowly.  
4. Manually connect to the WiFi SSID named BroadlinkProv.  
5. Run setup() and provide your ssid, network password (if secured), and set the security mode.    


    using SharpBroadlink;
     
    // Security mode options are [None, Wep, WPA1, WPA2, WPA12]    
    Broadlink.Setup('myssid', 'mynetworkpass', Broadlink.WifiSecurityMode.WPA12);


ex) Discover devices:  


    using SharpBroadlink;
     
    var devices = await Broadlink.Discover(5);


ex) Get signal data:


    using SharpBroadlink;
     
    var devices = await Broadlink.Discover(5);
    var rm = (SharpBroadlink.Devices.Rm) devices[0];
     
    // Enter Learning mode
    await rm.EnterLearning();


Here, Point the remote control to be learned to RM Pro, and press the button.  
And...  


    // Get signal data.
    var signal = await rm.CheckData();
     
    // Test signal
    await rm.SendData(signal);


## Licence

[MIT Licence](https://github.com/ume05rw/SharpBroadlink/blob/master/LICENSE)

## Links

Original python-broadlink:  
[https://github.com/mjg59/python-broadlink](https://github.com/mjg59/python-broadlink)  
  
Protocol document:  
[https://github.com/mjg59/python-broadlink/blob/master/protocol.md](https://github.com/mjg59/python-broadlink/blob/master/protocol.md)  
