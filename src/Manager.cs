using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace com.janoserdelyi.Devicer
{
	public class Manager
	{
		private Manager () {

		}

		public static Manager Instance {
			get {
				if (instance == null) {
					throw new NullReferenceException ("No instance exists. Load() one first");
				}
				return instance;
			}
		}

		public static Manager Load (
			string deviceListPath
		) {
			if (instance == null) {
				instance = new Manager ();
				instance.loadList (deviceListPath);
			}
			return instance;
		}

		public void AddSource (
			string deviceListPath
		) {
			if (instance == null) {
				throw new Exception ("The Devicer has not been loaded yet. please Load() first");
			}
			instance.loadList (deviceListPath);
		}

		public Device GetDevice (
			string useragent
		) {

			Device dev = new Device () {
				UserAgent = useragent,
				BoiledUserAgent = useragent,
				DeviceType = DeviceType.Unknown
			};

			if (string.IsNullOrEmpty (useragent)) {
				throw new ArgumentException ("Empty or null User Agent strings are not allowed");
			}

			useragent = Manager.BoilIt (useragent);
			Console.WriteLine ("Distilled : " + useragent);

			if (boiledList.ContainsKey (useragent)) {
				dev = boiledList[useragent];
				dev.BestGuess = dev.DeviceType;
				return dev;
			}

			dev.BoiledUserAgent = useragent;

			//ok, at this point we have an unknown device. i may inject some best-guesses on things i can largely count on. example: if "ipad;" is in the UA, it's a tablet

			dev = BestGuess (dev);

			return dev;
		}

		public static Device BestGuess (
			Device dev
		) {
			//dev.BoiledUserAgent = BoilIt (dev.UserAgent);
			// nope, going to assume it's been boiled

			if (dev.BestGuess != DeviceType.Unknown) {
				return dev;
			}

			if (dev.BoiledUserAgent.Contains ("ipad;")) {
				dev.BestGuess = DeviceType.Tablet;
				return dev;
			}
			if (dev.BoiledUserAgent.Contains ("tablet") || dev.BoiledUserAgent.Contains ("transformer")) {
				dev.BestGuess = DeviceType.Tablet;
				return dev;
			}

			// samsung is their own brand of 'special'
			if (dev.BoiledUserAgent.IndexOf ("(sm-") > -1) {
				// 2018-01-11 samsung tablets giver NO indication that they are tablets - except model numbers
				// since samsungs are pretty widespread for tablets i'll make this exception
				// there are other patterns but most dating back to 2013 follow this pattern
				if (Regex.IsMatch (dev.BoiledUserAgent, "sm\\-t[\\d]{3}")) {
					dev.BestGuess = DeviceType.Tablet;
					return dev;
				}
				if (Regex.IsMatch (dev.BoiledUserAgent, "sm\\-p[\\d]{3}")) {
					dev.BestGuess = DeviceType.Tablet;
					return dev;
				}
				// since these are phones, i'll skip the checks
				/*
				if (Regex.IsMatch (dev.BoiledUserAgent, "sm\\-s[\\d]{3}")) {
					dev.BestGuess = DeviceType.Phone;
					return dev;
				}
				if (Regex.IsMatch (dev.BoiledUserAgent, "sm\\-n[\\d]{3}")) {
					dev.BestGuess = DeviceType.Phone;
					return dev;
				}
				if (Regex.IsMatch (dev.BoiledUserAgent, "sm\\-j[\\d]{3}")) {
					dev.BestGuess = DeviceType.Phone;
					return dev;
				}
				if (Regex.IsMatch (dev.BoiledUserAgent, "sm\\-g[\\d]{3}")) {
					dev.BestGuess = DeviceType.Phone;
					return dev;
				}
				if (Regex.IsMatch (dev.BoiledUserAgent, "sm\\-a[\\d]{3}")) {
					dev.BestGuess = DeviceType.Phone;
					return dev;
				}
				*/
			}

			if (dev.BoiledUserAgent.Contains ("phone") || dev.BoiledUserAgent.Contains ("mobile")) {
				dev.BestGuess = DeviceType.Phone;
				return dev;
			}
			if (dev.BoiledUserAgent.Contains ("touch")) { // we'll see how this goes... 2013-02-26
				dev.BestGuess = DeviceType.Tablet;
				return dev;
			}

			if (dev.BoiledUserAgent.Contains ("windows nt") || dev.BoiledUserAgent.Contains ("desktop")) {
				dev.BestGuess = DeviceType.Desktop;
				return dev;
			}
			//android 3.x is only for tablets
			if (dev.BoiledUserAgent.Contains ("android 3")) {
				dev.BestGuess = DeviceType.Tablet;
				return dev;
			}

			//kindle browser
			if (dev.BoiledUserAgent.Contains ("silk")) {
				dev.BestGuess = DeviceType.Tablet;
				return dev;
			}

			// the google nexus line... many are tablets, but they have phones too. default to tablet if unknown
			if (dev.BoiledUserAgent.Contains ("android") && dev.BoiledUserAgent.Contains ("nexus")) {
				if (dev.BoiledUserAgent.Contains ("nexus 4") || dev.BoiledUserAgent.Contains ("nexus 5")) {
					dev.BestGuess = DeviceType.Phone;
					return dev;
				}
				dev.BestGuess = DeviceType.Tablet;
				return dev;
			}

			// still unknown? android? phone it is then
			if (dev.BoiledUserAgent.Contains ("android")) {

				if (dev.BoiledUserAgent.Contains ("pixel")) {
					dev.BestGuess = DeviceType.Phone;
					return dev;
				}

				dev.BestGuess = DeviceType.Phone;
				return dev;
			}

			if (dev.BoiledUserAgent.Contains ("x86")) {
				dev.BestGuess = DeviceType.Desktop;
				return dev;
			}
			if (
				dev.BoiledUserAgent.IndexOf ("i386") > -1 ||
				dev.BoiledUserAgent.IndexOf ("i586") > -1 ||
				dev.BoiledUserAgent.IndexOf ("i686") > -1
			) {
				dev.BestGuess = DeviceType.Desktop;
				return dev;
			}

			if (dev.BoiledUserAgent.Contains ("intel mac os x")) {
				dev.BestGuess = DeviceType.Desktop;
				return dev;
			}

			return dev;
		}

		public static string BoilIt (
			string useragent
		) {
			if (string.IsNullOrEmpty (useragent)) {
				return "";
			}

			useragent = useragent.ToLower ().Trim ();
			//there are some like "CDM-8400" as the ENTIRE UA string. for very short strings, they are basically going to be passed on as-is
			// revising this out. it won't help me with things like "curl/1.12" where i want just "curl" but i have version info jamming it up
			//if (useragent.Length < 20) {
			//return useragent;
			//}

			Regex reg = new Regex (@"(?<named>[a-z\-\d]+)/[a-z\d\.]+");
			useragent = reg.Replace (useragent, "${named}");
			Regex lang = new Regex (@"\s{1}[a-z]{2}-[a-z]{2};?"); //strip language/locale bits
			useragent = lang.Replace (useragent, "");
			Regex locale = new Regex (@"\[[a-z]{2}(-[a-z]{2}){0,1}\]"); //found a fair number like [en-US] and [en]
			useragent = locale.Replace (useragent, "");
			// boil all deep versions to major versions
			Regex major = new Regex (@"(?<major>\d{1,})(\.|_)\d{1,}((\.|_)\d{1,}){0,}");
			useragent = major.Replace (useragent, "${major}");

			// Opera has some version formats which seem to be "Opera Mobi/(ADR|SYB|build|etc)(-)?000000000000;"
			Regex operaVersion = new Regex (@"\sOpera\sMobi(/?[a-z]*?\-?[\d]{1,});?", RegexOptions.IgnoreCase);
			useragent = operaVersion.Replace (useragent, " mobile;");

			//tired of seeing dangling "version"
			useragent = Regex.Replace (useragent, @"\s?version\s?", "");

			// 2014-06-11. i'm not sure revisions matter. if i can tell something is a desktop browser, it's not because the version is 12 or 30
			// starting light with rv:123abc removals
			useragent = Regex.Replace (useragent, @"[\W]{1,}rv:[^)\W]{1,10}", "");
			// removing more versions. msie 9; to just msie; windows nt 6; to just windows nt; retain the semi-colon for now
			useragent = Regex.Replace (useragent, @"[\W]{1}[\d]{1,};", ";");
			// more version removal!! danglers like " 5732)". leave the parenthesis
			useragent = Regex.Replace (useragent, @"\W{1}[\d]{1,}\)", ")");

			// seeing far too much (khtml, like gecko) - remove it
			useragent = useragent.Replace ("(khtml, like gecko)", "");

			useragent = useragent.Replace (" build;", "");

			useragent = useragent.Replace ("linux; android; ", "");

			useragent = useragent.Replace (" wv)", ")");

			// some spaces before closing parens
			useragent = Regex.Replace (useragent, @"\s{1,}\)", ")");

			//useragent = useragent.Replace ("[pinterest]", "");
			if (useragent.IndexOf ('[') > -1) {
				//Console.WriteLine (Regex.IsMatch (useragent, @"(\[[^\]]+\])+"));
				useragent = Regex.Replace (useragent, @"(\[[^\]]+\])+", "");
			}

			// remove multiple spaces
			useragent = Regex.Replace (useragent, @"\s{2,}", " ").Trim ();

			return useragent;
		}

		// the major change in this from the previous version is that i am only storing the boiled versions
		// this should drastically reduce the number of items in the list and still give reasonable hits
		// i may also pull from our database and boil them down to inject more real-world examples
		private void loadList (
			string deviceListPath
		) {
			if (string.IsNullOrEmpty (deviceListPath)) {
				throw new ArgumentNullException ("deviceListPath is required");
			}

			if (!File.Exists (deviceListPath)) {
				throw new FileNotFoundException ("The device list file cannot be found at " + deviceListPath);
			}

			System.Xml.XmlDocument doc = new System.Xml.XmlDocument ();
			doc.Load (deviceListPath);

			foreach (XmlNode dnode in doc.SelectNodes ("//boiled/device")) {
				Device dev = new Device ();
				dev.UserAgent = dnode.Attributes["ua"].Value;
				dev.BoiledUserAgent = dnode.Attributes["ua"].Value;

				if (string.IsNullOrEmpty (dev.BoiledUserAgent)) {
					continue;
				}

				switch (dnode.Attributes["type"].Value) {
					case "Phone":
						dev.DeviceType = DeviceType.Phone;
						break;
					case "Tablet":
						dev.DeviceType = DeviceType.Tablet;
						break;
					case "Desktop":
						dev.DeviceType = DeviceType.Desktop;
						break;
					case "Unknown":
						dev.DeviceType = DeviceType.Unknown;
						break;
				}

				if (!boiledList.ContainsKey (dev.BoiledUserAgent)) {
					boiledList.Add (dev.BoiledUserAgent, dev);
				} else {
					// curiosity check. i don't want one boiled ua to override the device type if there are overlaps. i want to know about them, so explode
					if (boiledList[dev.BoiledUserAgent].DeviceType != dev.DeviceType) {
						throw new NotImplementedException ("mismatch/overlap! boiled UA has multiple device types : '" + dev.BoiledUserAgent + "'");
					}
				}
			}

		}

		private static Manager instance { get; set; }
		private IDictionary<string, Device> boiledList { get; set; } = new Dictionary<string, Device> ();
	}
}