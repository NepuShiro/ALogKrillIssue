using System.IO.Pipes;
using System.Text;
using System.Text.RegularExpressions;

namespace LogViewer
{
	public class LogViewer
	{
		private static readonly string pipeName = "LogPipe";
		private static bool stopping = false;
		private static ConsoleColor currentColor = ConsoleColor.Gray;

		static void Main()
		{
			Console.WriteLine("LogViewer started. Press Any Key to exit...");

			Task.Run(() => ReadFromPipeAsync());

			Console.ReadLine();
			stopping = true;
		}

		private static async Task ReadFromPipeAsync()
		{
			while (!stopping)
			{
				try
				{
					using NamedPipeClientStream pipeClient = new(".", pipeName, PipeDirection.In, PipeOptions.Asynchronous);
					Console.WriteLine("Attempting to connect to named pipe server...");
					await pipeClient.ConnectAsync(1000);
					Console.WriteLine("Connected to named pipe server.");

					using StreamReader reader = new(pipeClient, Encoding.UTF8);
					while (!stopping)
					{
						string? response = await reader.ReadLineAsync();
						if (response == null)
							break;

						OnLogMessage(response);
					}
				}
				catch (TimeoutException)
				{
					// Console.WriteLine("Connection to named pipe server timed out. Retrying...");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error in named pipe client: {ex.Message}");
				}

				if (!stopping)
				{
					await Task.Delay(1000);
				}
			}
		}

		private static void OnLogMessage(string message)
		{
			string cleanedMessage = RemoveTimestampAndFPS(message);
			
			if (string.IsNullOrWhiteSpace(cleanedMessage))
			{
				currentColor = ConsoleColor.Gray;
				Console.WriteLine();
				return;
			}

			if (HasTimestampAndFPS(message))
			{
				if (IsValidLog(cleanedMessage) && MatchesLogPatterns(cleanedMessage))
				{
					FormatModLoaderLog(cleanedMessage);
				}
				else if (IsValidLog(cleanedMessage))
				{
					Console.ForegroundColor = ConsoleColor.Gray;
					Console.WriteLine($"{cleanedMessage}");
					Console.ResetColor();
				}
			}
			else
			{
				// This is a continuation of the previous message
				Console.ForegroundColor = currentColor;
				Console.WriteLine($"{cleanedMessage}");
				Console.ResetColor();
			}
		}

		private static bool HasTimestampAndFPS(string message)
		{
			string pattern = @"\d{1,2}:\d{1,2}:\d{1,2} [APap][Mm]\.\d{1,3}\s+\(\s*-*\d+\s?FPS\s?\)";
			return Regex.IsMatch(message, pattern);
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
			Console.ForegroundColor = consoleColor;
			Console.WriteLine($"{message}");
			Console.ResetColor();
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

		private static string RemoveTimestampAndFPS(string message)
		{
			string timestampPattern = @"\d{1,2}:\d{1,2}:\d{1,2} [APap][Mm]\.\d{1,3}\s";
			string fpsPattern = @"\(\s*-*\d+\s?FPS\s?\)\s+";

			message = Regex.Replace(message, timestampPattern, "");
			message = Regex.Replace(message, fpsPattern, "");

			return message;
		}
	}
}