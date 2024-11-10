using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Elements.Core;
using ResoniteModLoader;

namespace ALogSender
{
	public class ALogSender : ResoniteMod
	{
		public override string Name => "ALogSender";
		public override string Author => "NepuShiro";
		public override string Version => "2.0.0";
		public override string Link => "https://github.com/NepuShiro/ALogKrillIssue";
		private static UdpClient udpClient;
		
		[AutoRegisterConfigKey]
		public static readonly ModConfigurationKey<int> PORT = new ModConfigurationKey<int>("Port", "The Port for the udpClient to broadcast on", () => 9999);

		public static ModConfiguration config;

		public override void OnEngineInit()
		{
			config = GetConfiguration();
			config.Save(true);

			StartUDPClient();
			config.OnThisConfigurationChanged += (c) => 
			{
				if (c.Key == PORT)
				{
					RestartUDPClient();
				}
			};
		}

		private static void StartUDPClient()
		{
			try
			{
				Msg("UDP Client started. Listening...");
				udpClient = new UdpClient() 
				{
					EnableBroadcast = true,
				};
				udpClient.Connect("255.255.255.255", config.GetValue(PORT));

				UniLog.OnLog += WriteToUDP;
				UniLog.OnWarning += WriteToUDP;
				UniLog.OnError += WriteToUDP;
			}
			catch (Exception ex)
			{
				Error($"Error in UDP Client: {ex.Message}");
				RestartUDPClient();
			}
		}

		private static void WriteToUDP(string message)
		{
			try
			{
				byte[] sendBytes = Encoding.UTF8.GetBytes(message);
				udpClient.Send(sendBytes, sendBytes.Length);
			}
			catch (Exception ex)
			{
				Error($"Error sending message to UDP Server: {ex.Message}");
				RestartUDPClient();
			}
		}
		
		private static void RestartUDPClient()
		{
			udpClient.Dispose();
			udpClient.Close();
			
			UniLog.OnLog -= WriteToUDP;
			UniLog.OnWarning -= WriteToUDP;
			UniLog.OnError -= WriteToUDP;
			
			StartUDPClient();
		}
	}
}
