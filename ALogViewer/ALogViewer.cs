using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace ALogViewer
{
	public class ALogViewer
	{
		private static bool stopping = false;
		private static ConsoleColor currentColor = ConsoleColor.Gray;
		private static UdpClient udpClient;
		private static string lastLogMessage = string.Empty;
		private static readonly string pattern = @"\d{1,2}:\d{1,2}:\d{1,2}(\s[APap][Mm])?\.\d{1,3}\s+\(\s*-*\d+\s?FPS\s?\)\s";

		static void Main(string[] args)
		{
			try
			{
				int port = 9999;
				try
				{
					port = args.Length > 0 ? int.Parse(args[0]) : 9999;
				}
				catch (Exception e)
				{
					PrintMessage($"Error parsing Port: {e.Message}", ConsoleColor.Red);
					PrintMessage($"Using default Port: {port}", ConsoleColor.Red);
					port = 9999;
				}

				udpClient = new UdpClient();
				udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
				udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));

				Console.Title = "Resonite Console";
				PrintMessage("LogViewer started. Press Enter to exit...");

				Task.Run(ReceiveLogsAsync);
			}
			catch (Exception e)
			{
				PrintMessage($"An Error has occured durring Init: {e}", ConsoleColor.Red);
			}

			Console.ReadLine();
			stopping = true;
			udpClient.Dispose();
			udpClient.Close();
		}

		private static async Task ReceiveLogsAsync()
		{
			while (!stopping)
			{
				try
				{
					UdpReceiveResult result = await udpClient.ReceiveAsync();
					byte[] receiveBytes = result.Buffer;
					string receivedMessage = Encoding.UTF8.GetString(receiveBytes);

					if (!string.IsNullOrWhiteSpace(receivedMessage))
					{
						OnLogMessage(receivedMessage);
						lastLogMessage = receivedMessage;
					}
				}
				catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset)
				{
					string disconnectMessage = "Disconnected from server. Attempting to reconnect...";
					if (lastLogMessage != disconnectMessage)
					{
						PrintMessage(disconnectMessage, ConsoleColor.Red);
						lastLogMessage = disconnectMessage;
					}
					await Task.Delay(1000);
				}
				catch (ObjectDisposedException) when (stopping)
				{
					break;
				}
				catch (Exception ex)
				{
					PrintMessage($"Error receiving log entry: {ex.Message}", ConsoleColor.Red);
				}
			}
		}

		private static bool IsValidText(string message)
		{
			foreach (char c in message)
			{
				if (c > 127)
				{
					return false;
				}
			}
			return true;
		}

		private static void OnLogMessage(string message)
		{
			string cleanedMessage = Regex.Replace(message, pattern, "");
			bool hasTimestampAndFPS = Regex.IsMatch(message, pattern);
			if (IsValidText(cleanedMessage))
			{
				if (hasTimestampAndFPS)
				{
					if (IsValidLog(cleanedMessage) && MatchesLogPatterns(cleanedMessage))
					{
						FormatModLoaderLog(cleanedMessage);
					}
					else if (IsValidLog(cleanedMessage))
					{
						PrintMessage(cleanedMessage);
					}
				}
				else
				{
					// This is a continuation of the previous message
					PrintMessage(cleanedMessage, currentColor);
				}
			}
		}

		private static void FormatModLoaderLog(string message)
		{
			var lower = message.ToLower();
			var consoleColor = Regex.IsMatch(lower, @"\[error\]") ? ConsoleColor.Red :
							   Regex.IsMatch(lower, @"failed load: could not gather|exception") ? ConsoleColor.Red :
							   Regex.IsMatch(lower, @"exception in runningcoroutine") ? ConsoleColor.Red :
							   Regex.IsMatch(lower, @"<\w{32}>:0") ? ConsoleColor.Red :
							   Regex.IsMatch(lower, @"\[info\]") ? ConsoleColor.Green :
							   Regex.IsMatch(lower, @"\[debug\]|\[trace\]|resonite (unity) game pack") ? ConsoleColor.Blue :
							   Regex.IsMatch(lower, @"\[warn\]|updated: https|lastmodifyinguser|broadcastkey") ? ConsoleColor.Yellow :
							   Regex.IsMatch(lower, @"signalr|clearing expired status|status before clearing:|status after clearing:|status initialized|updated:  ->") ? ConsoleColor.DarkMagenta :
							   Regex.IsMatch(lower, @"sendstatustouser:") ? ConsoleColor.Magenta :
							   Regex.IsMatch(lower, @"running refresh on:") ? ConsoleColor.Cyan :
							   ConsoleColor.Gray;

			currentColor = consoleColor;
			PrintMessage(message, consoleColor);
		}

		private static bool MatchesLogPatterns(string message)
		{
			var lower = message.ToLower();
			return Regex.IsMatch(lower, @"monkeyloader|broadcastkey|running refresh on:|\[info\]|\[debug\]|\[trace\]|\[error\]|\[warn\]|updated: https|lastmodifyinguser|failed load: could not gather|signalr|clearing expired status|status before clearing:|status after clearing:|updated:  ->|updated: 1/1/0001 12:00:00 am ->|status initialized|sendstatustouser:|exception|exception in runningcoroutine|<\w{32}>:0");
		}

		private static bool IsValidLog(string message)
		{
			string lower = message.ToLower();
			return !Regex.IsMatch(lower, "session updated, forcing status update") &&
				   !Regex.IsMatch(lower, @"\[debug\]\[resonitemodloader\] intercepting call to appdomain\.getassemblies\(\)") &&
				   !Regex.IsMatch(lower, "rebuild:");
		}
		
		private static void PrintMessage(string message, ConsoleColor color = ConsoleColor.Gray)
		{
			Console.ForegroundColor = color;
			Console.WriteLine(message);
			Console.ResetColor();
		}
	}
}