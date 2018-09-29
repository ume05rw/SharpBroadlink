SharpBroadlink - for Broadlink RM IR Controller
====

A simple C# API for controlling RM from [Broadlink](http://www.ibroadlink.com/rm/).  
This is a port of [python-broadlink](https://github.com/mjg59/python-broadlink) to C# .Net Standard.  

## Description

Supported device:  
* RM Pro  
* RM mini 3 (Black Bean)  
* A1 Temperature/Humidity/Noise/Light/VOC Sensor   
* SP mini 3 Smart Plug
  
Supports .NET Standard2.0

## Requirement
[Xb.Core](https://www.nuget.org/packages/Xb.Core/)  
[Xb.Net](https://github.com/ume05rw/Xb.Net/)  

## Usage
1. Create your new C# project.
2. Download this solution.
3. Add a project-reference SharpBroadlink/SharpBroadlink.csproj to your project.
4. Setup devices or discover devices as follows:


#### Setup:  

== Preparation - It is the [same as the original](https://github.com/mjg59/python-broadlink). ==  

1. Put the device into AP Mode.  
2. Long press the reset button until the blue LED is blinking quickly.  
3. Long press again until blue LED is blinking slowly.  
4. Manually connect to the WiFi SSID named BroadlinkProv.  

== Preparation is over ==

    using SharpBroadlink;
     
    // Security mode options are [None, Wep, WPA1, WPA2, WPA12]    
    Broadlink.Setup('myssid', 'mynetworkpass', Broadlink.WifiSecurityMode.WPA12);


#### Discover devices:  


    using SharpBroadlink;
     
    var devices = await Broadlink.Discover(5);


#### Get signal data with RM:


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

#### Get sensor data with A1:

    using SharpBroadlink;
     
    var devices = await Broadlink.Discover(5);
    var device = (SharpBroadlink.Devices.A1)devs.First(d => d.DeviceType == DeviceType.A1);
    
    // before Auth, cannot get values. 
    await device.Auth();
     
    var values = await dev.CheckSensors();


#### Get/Set plug state with SP3:

    using SharpBroadlink;
     
    var devices = await Broadlink.Discover(5);
    var device = (SharpBroadlink.Devices.Sp2)devs.First(d => d.DeviceType == DeviceType.Sp2);
    
    // before Auth, cannot get values. 
    await device.Auth();
     
    var powerState = await dev.CheckPower();
    var nightLightState = await dev.CheckNightLight();

    await dev.SetPower(!powerState);
    await dev.SetNightLight(!nightLightState);

## Licence

[MIT Licence](https://github.com/ume05rw/SharpBroadlink/blob/master/LICENSE)

## Links

Original python-broadlink:  
[https://github.com/mjg59/python-broadlink](https://github.com/mjg59/python-broadlink)  
  
Protocol document:  
[https://github.com/mjg59/python-broadlink/blob/master/protocol.md](https://github.com/mjg59/python-broadlink/blob/master/protocol.md)  
