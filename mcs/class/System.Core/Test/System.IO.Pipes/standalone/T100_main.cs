using System;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;

namespace MonoTests.System.IO.Pipes
{
  public class T100_Server_Main : PipeTestWrapper
  {
    public static int Main(string[] arguments) {
      return new T100_Server_Main().Execute(arguments);
    }

    public override string TestDescription {
      get {
        return @"Creates an anonymous pipe server stream, 
                 invokes the client application specified on the commandline, 
                 sends a single line of text across the anonymous pipe stream,
                 exits";
      }
    }

    protected override void DoTest(string[] arguments)
    {
      string clientExe = null;
      foreach (string arg in arguments) {
        if (arg.StartsWith("/client:")) {
          clientExe = arg.Substring("/client:".Length);
        }
      }
      if (clientExe == null) {
        _log.Error("Usage: /client:<clientexe>");
        _log.Error("  clientexe - exe that should be invoked with the anonymous pipe handle");
        return;
      }

      _log.Info("Working directory {0}", Environment.CurrentDirectory);

      Process pipeClient = new Process();
      pipeClient.StartInfo.FileName = clientExe;
      pipeClient.StartInfo.WorkingDirectory = Environment.CurrentDirectory;

      using (AnonymousPipeServerStream pipeServer = new AnonymousPipeServerStream(PipeDirection.Out))
      {
        pipeClient.StartInfo.Arguments = FormatArguments("/handle:" + pipeServer.GetClientHandleAsString());
        pipeClient.StartInfo.UseShellExecute = false;
        pipeClient.Start();

        pipeServer.DisposeLocalCopyOfClientHandle();

        _log.Info("Sending message");
        using (StreamWriter writer = new StreamWriter(pipeServer)) 
        {
          writer.AutoFlush = true;
          writer.WriteLine("Message sent from anonymous pipe server stream");
        }
        _log.Info("Message sending completed");
      }

      pipeClient.WaitForExit();
    }
  }
}
