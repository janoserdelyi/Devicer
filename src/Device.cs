using System;

namespace com.janoserdelyi.Devicer
{
	public class Device
	{
		public string UserAgent { get; set; }
		public string BoiledUserAgent { get; set; }
		public DeviceType DeviceType { get; set; } = DeviceType.Unknown;
		public DeviceType BestGuess { get; set; } = DeviceType.Unknown;
	}
}