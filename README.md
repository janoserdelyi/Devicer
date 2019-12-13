# Devicer
Reductionist device-type detection

Old project cleaned up some.

Basically it just reduces User-Agent header values to remove things like version info so that you can reliably know that a stable, reduced UA belongs to a certain class of device (Desktop, Tablet, Phone)

rough example : 

```csharp
using com.janoserdelyi.Devicer;

// get a device manager
DeviceManager = Manager.Load ("path/to/your/devicer.xml");

// if you have additional files you wish to load
DeviceManager.AddSource ("path/to/your/devicer-addon.xml");

// get a device based off the useragent passed in
Device device = DeviceManager.GetDevice ("Some User-Agent string here");

// "device" will contain an enum for DeviceType
// it will also have an enum for "BestGuess" if it doesn't find the device directly in the xml provided

```
