using System;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using System.Threading;

namespace MonoTests.System.IO.Pipes
{
  public class T114_Server_Client : PipeTestWrapper
  {
    public static int Main(string[] arguments) {
      return new T114_Server_Client().Execute(arguments);
    }

    public override string TestDescription {
      get {
        return @"Creates an anonymous in pipe client stream from the handle passed on the commandline, 
                 and then generates an error if the provided handle actually exists.";
      }
    }

    protected override void DoTest(string[] arguments)
    {
      InheritedContextSwitchTool();

      string inHandle = null;
      foreach (string arg in arguments)
      {
        if (arg.StartsWith("/inHandle:")) {
          inHandle = arg.Substring("/inHandle:".Length);
        }
      }
      if (inHandle == null) {
        _log.Error("Usage: /inHandle:<pipehandle>");
        _log.Error("  inHandle - handle generated by GetClientHandleAsString() which will be read from");
        return;
      }

      _log.Test("Setting up streams");
      _log.Info("Received in handle {0}", inHandle);
  
      try {
        using (AnonymousPipeClientStream pipeClientIn = new AnonymousPipeClientStream(PipeDirection.In, inHandle))
        {
          _log.Error("Expected an error to be generated before reaching this point");
        }
      } catch (IOException eError) {
        if (eError.Message != "Unable to load pipe with error code ERROR_INVALID_HANDLE") {
          _log.Error(eError, "Unexpected io exception");
        } else {
          _log.Test("Received expected exception");
        } 
      }

      _log.Test("Finished disposing pipe streams");
    }
  }
}
