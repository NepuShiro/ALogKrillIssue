using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using ResoniteModLoader;
using Elements.Core;

namespace ALogSender
{
	public class ALogSender : ResoniteMod
	{
		public override string Name => "ALogSender";
		public override string Author => "NepuShiro";
		public override string Version => "1.5.0";
		public override string Link => "https://github.com/NepuShiro/ALogKrillIssue";

		private static readonly string pipeName = "LogPipe";
		private static NamedPipeServerStream pipeServer;
		private static StreamWriter writer;
		private static bool isRunning = true;

		public override void OnEngineInit()
		{
			Msg("-------------------- OnEngineInit --------------------");

			Task.Run(() => StartPipeServer());
		}

		private static void StartPipeServer()
		{
			try
			{
				while (isRunning)
				{
					pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.Out);

					Msg("Named pipe server started. Waiting for client connection...");

					pipeServer.WaitForConnection();

					Msg("Client connected to named pipe.");

					UniLog.OnLog += WriteToPipe;
					UniLog.OnWarning += WriteToPipe;
					UniLog.OnError += WriteToPipe;

					writer = new StreamWriter(pipeServer, Encoding.UTF8) { AutoFlush = true };
				}
			}
			catch (Exception ex)
			{
				Error($"Error in named pipe server: {ex.Message}");
			}
		}

		private static async void WriteToPipe(string message)
		{
			try
			{
				if (writer != null && pipeServer != null && pipeServer.IsConnected)
				{
					writer.WriteLine(message);
				}
			}
			catch (IOException)
			{
				isRunning = false;
				pipeServer.Disconnect();

				UniLog.OnLog -= WriteToPipe;
				UniLog.OnWarning -= WriteToPipe;
				UniLog.OnError -= WriteToPipe;

				writer.Flush();
				writer.Dispose();
				writer.Close();
				writer = null;

				pipeServer.WaitForPipeDrain();
				pipeServer.Dispose();
				pipeServer.Close();
				pipeServer = null;

				isRunning = true;
				await Task.Run(() => StartPipeServer());
			}
		}
	}
}
