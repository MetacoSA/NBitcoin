using NBitcoin.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class alert_tests
	{

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanParseAlertAndChangeUpdatePayloadAndSignature()
		{
			var alertStr = "73010000003766404f00000000b305434f00000000f2030000f1030000001027000048ee00000064000000004653656520626974636f696e2e6f72672f666562323020696620796f7520686176652074726f75626c6520636f6e6e656374696e67206166746572203230204665627275617279004730450221008389df45f0703f39ec8c1cc42c13810ffcae14995bb648340219e353b63b53eb022009ec65e1c1aaeec1fd334c6b684bde2b3f573060d5b70c3a46723326e4e8a4f1";
			AlertPayload alert = new AlertPayload();
			alert.FromBytes(TestUtils.ParseHex(alertStr));
			Assert.True(alert.CheckSignature(Network.Main));
			Assert.Equal("See bitcoin.org/feb20 if you have trouble connecting after 20 February", alert.StatusBar);
			alert.StatusBar = "Changing...";
			Assert.True(alert.CheckSignature(Network.Main));
			alert.UpdatePayload();
			Assert.False(alert.CheckSignature(Network.Main));
			Key key = new Key();
			alert.UpdateSignature(key);
			Assert.True(alert.CheckSignature(key.PubKey));
		}

		[Fact]
		[Trait("Core", "Core")]
		public void AlertApplies()
		{
			var alerts = ReadAlerts();
			foreach(var alert in alerts)
			{
				Assert.True(alert.CheckSignature(Network.Main));
				Assert.True(!alert.CheckSignature(Network.TestNet));
				alert.Now = Utils.UnixTimeToDateTime(11);
			}


			Assert.True(alerts.Length >= 3);



			// Matches:
			Assert.True(alerts[0].AppliesTo(1, ""));
			Assert.True(alerts[0].AppliesTo(999001, ""));
			Assert.True(alerts[0].AppliesTo(1, "/Satoshi:11.11.11/"));

			Assert.True(alerts[1].AppliesTo(1, "/Satoshi:0.1.0/"));
			Assert.True(alerts[1].AppliesTo(999001, "/Satoshi:0.1.0/"));

			Assert.True(alerts[2].AppliesTo(1, "/Satoshi:0.1.0/"));
			Assert.True(alerts[2].AppliesTo(1, "/Satoshi:0.2.0/"));

			// Don't match:
			Assert.True(!alerts[0].AppliesTo(-1, ""));
			Assert.True(!alerts[0].AppliesTo(999002, ""));

			Assert.True(!alerts[1].AppliesTo(1, ""));
			Assert.True(!alerts[1].AppliesTo(1, "Satoshi:0.1.0"));
			Assert.True(!alerts[1].AppliesTo(1, "/Satoshi:0.1.0"));
			Assert.True(!alerts[1].AppliesTo(1, "Satoshi:0.1.0/"));
			Assert.True(!alerts[1].AppliesTo(-1, "/Satoshi:0.1.0/"));
			Assert.True(!alerts[1].AppliesTo(999002, "/Satoshi:0.1.0/"));
			Assert.True(!alerts[1].AppliesTo(1, "/Satoshi:0.2.0/"));

			Assert.True(!alerts[2].AppliesTo(1, "/Satoshi:0.3.0/"));

		}

		private AlertPayload[] ReadAlerts()
		{
			List<AlertPayload> alerts = new List<AlertPayload>();
			using(var fs = File.OpenRead("data/alertTests.raw"))
			{
				BitcoinStream stream = new BitcoinStream(fs, false);
				while(stream.Inner.Position != stream.Inner.Length)
				{
					AlertPayload payload = null;
					stream.ReadWrite(ref payload);
					alerts.Add(payload);
				}
			}
			return alerts.ToArray();
		}
	}
}
