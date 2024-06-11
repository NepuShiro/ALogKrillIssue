using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using ResoniteModLoader;
using Elements.Core;

namespace ALogSender
{
    public class ALogSender : ResoniteMod
    {
        public override string Name => "ALogSender";
        public override string Author => "NepuShiro";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/NepuShiro/ALogSender";

        private static readonly string pipeName = "LogPipe";
        private static NamedPipeServerStream pipeServer;
        private static StreamWriter writer;

        public override void OnEngineInit()
        {
            Msg("-------------------- OnEngineInit --------------------");

            Thread pipeServerThread = new Thread(StartPipeServer);
            pipeServerThread.Start();

            while (writer == null)
            {
                Thread.Sleep(100);
            }

            UniLog.OnLog += WriteToPipe;
            UniLog.OnWarning += WriteToPipe;
            UniLog.OnError += WriteToPipe;
        }

        private static void StartPipeServer()
        {
            try
            {
                pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.Out);

                Msg("Named pipe server started. Waiting for client connection...");

                pipeServer.WaitForConnection();

                Msg("Client connected to named pipe.");

                writer = new StreamWriter(pipeServer, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Error($"Error in named pipe server: {ex.Message}");
            }
        }

        private static void WriteToPipe(string message)
        {
            try
            {
                if (writer != null && pipeServer.IsConnected)
                {
                    writer.WriteLine(message);
                    writer.Flush();
                }
            }
            catch (Exception ex)
            {
                Error($"Error writing to pipe: {ex.Message}");
            }
        }
    }
}
