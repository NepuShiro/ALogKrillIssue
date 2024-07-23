using System;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Elements.Core;
using ResoniteModLoader;

namespace ALogSender
{
	public class ALogSender : ResoniteMod
	{
		public override string Name => "ALogSender";
		public override string Author => "NepuShiro";
		public override string Version => "1.0.5";
		public override string Link => "https://github.com/NepuShiro/ALogKrillIssue";
		private static UdpClient udpClient;

		public override void OnEngineInit()
		{
			Msg("-------------------- OnEngineInit --------------------");

			Task.Run(() => StartPipeServer());
		}

		private static void StartPipeServer()
		{
			try
			{
				udpClient = new UdpClient();
				udpClient.Connect(new IPEndPoint(IPAddress.Loopback, 9999));

				Msg("Unix domain socket server started. Listening...");

				UniLog.OnLog += WriteToSocket;
				UniLog.OnWarning += WriteToSocket;
				UniLog.OnError += WriteToSocket;
			}
			catch (Exception ex)
			{
				Error($"Error in Unix domain socket server: {ex.Message}");
			}
		}

		private static void WriteToSocket(string message)
		{
			try
			{
				byte[] sendBytes = Encoding.UTF8.GetBytes(message);
				udpClient.Send(sendBytes, sendBytes.Length);
			}
			catch (Exception ex)
			{
				Error($"Error sending message to socket: {ex.Message}");
				
				udpClient.Dispose();
				udpClient.Close();
				
				UniLog.OnLog -= WriteToSocket;
				UniLog.OnWarning -= WriteToSocket;
				UniLog.OnError -= WriteToSocket;
				
				Task.Run(() => StartPipeServer());
			}
		}
	}
}
