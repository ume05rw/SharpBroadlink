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
1. [Add NuGet-Package](https://www.nuget.org/packages/SharpBroadlink/) to your project, or download this source and add ref [SharpBroadlink.csproj](https://github.com/ume05rw/SharpBroadlink/blob/master/SharpBroadlink/SharpBroadlink.csproj)
2. Setup devices or discover devices as follows:


#### Setup:  

== Preparation - It is the [same as the original](https://github.com/mjg59/python-broadlink). ==  

1. Plug the USB-cable into your Broadlink device, turn it on.  
2. Long press the reset button until the blue LED is blinking quickly.  
3. Long press again until blue LED is blinking slowly.  
4. Manually connect your PC to the WiFi SSID named BroadlinkProv.  

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
    var device = (SharpBroadlink.Devices.Rm)devices.First(d => d.DeviceType == DeviceType.Rm);
     
    await device.Auth();
     
    // Enter Learning mode
    await device.EnterLearning();


Here, Point the remote control to be learned to RM Pro, and press the button.  
And...  


    // Get signal data.
    var signal = await device.CheckData();
     
    // Test signal
    await device.SendData(signal);


#### Send Philips-Pronto format IR signal data with RM:

    using SharpBroadlink;
     
    var devices = await Broadlink.Discover(5);
    var device = (SharpBroadlink.Devices.Rm)devices.First(d => d.DeviceType == DeviceType.Rm);
    await device.Auth();
     
    // Toshiba-TV Power-On IR code
    // http://www.remotecentral.com/cgi-bin/codes/toshiba/ct-9726/
    var pronto = "0000 006b 0022 0002 0156 00ac 0016 0015 0016 0015 0016 0015 0016 0015 0016 0015 0016 0015 0016 0040 0016 0015 0016 0040 0016 0040 0016 0040 0016 0040 0016 0040 0016 0040 0016 0015 0016 0040 0016 0015 0016 0040 0016 0015 0016 0015 0016 0040 0016 0015 0016 0015 0016 0015 0016 0040 0016 0015 0016 0040 0016 0040 0016 0015 0016 0040 0016 0040 0016 0040 0016 05fb 0156 0056 0016 0e59";
    var bytes = Signals.String2ProntoBytes(pronto);
    
    await device.SendPronto(bytes);

#### Get sensor data with A1:

    using SharpBroadlink;
     
    var devices = await Broadlink.Discover(5);
    var device = (SharpBroadlink.Devices.A1)devices.First(d => d.DeviceType == DeviceType.A1);
    
    // before Auth, cannot get values. 
    await device.Auth();
     
    var values = await device.CheckSensors();


#### Get/Set plug state with SP3:

    using SharpBroadlink;
     
    var devices = await Broadlink.Discover(5);
    var device = (SharpBroadlink.Devices.Sp2)devices.First(d => d.DeviceType == DeviceType.Sp2);
    
    // before Auth, cannot get values. 
    await device.Auth();
     
    var powerState = await device.CheckPower();
    var nightLightState = await device.CheckNightLight();

    await device.SetPower(!powerState);
    await device.SetNightLight(!nightLightState);

## Licence

[MIT Licence](https://github.com/ume05rw/SharpBroadlink/blob/master/LICENSE)

## Links

Original python-broadlink:  
[https://github.com/mjg59/python-broadlink](https://github.com/mjg59/python-broadlink)  
  
Protocol document:  
[https://github.com/mjg59/python-broadlink/blob/master/protocol.md](https://github.com/mjg59/python-broadlink/blob/master/protocol.md)  
