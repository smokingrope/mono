using System;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonoTests.System.IO.Pipes
{
	public abstract class PipeTestWrapper
	{
		private TimeSpan _timeoutDuration;
		protected readonly TestLogger _log;
		private bool _executionError = false;
		private string[] _executionArgs = null;
		private ContextSwitchTool _cst;

		/// <summary>
		/// Instantiate pipe wrapper with timeout duration of 1 minute (the default)
		/// </summary>
		public PipeTestWrapper() :
			this (new TimeSpan(0, 0, 1, 0, 0))
		{
		}
		/// <summary>
		/// Instantiate pipe test wrapper with configurable timeout duration
		/// </summary>
		public PipeTestWrapper(TimeSpan timeoutDuration) {
			_timeoutDuration = timeoutDuration;
			_log = new TestLogger(this);
		}

		protected void InheritedContextSwitchTool() {
			_log.Info("Test is expecting inherited context switching tool");
			if (_cst == null) { throw new InvalidOperationException("Expected CST Tool to exist"); }
			if (!_cst.Inherited) {
				throw new ArgumentException("Context switch tool was not inherited from commandline");
			}
			_log.Info("Test found inherited context switching tool");
			ContextEnroll();
		}
		protected void CreatedContextSwitchTool() {
			_log.Info("Test is expecting fresh context switching tool");
			if (_cst == null) { throw new InvalidOperationException("Expected CST Tool to exist"); }
			if (_cst.Inherited) {
				throw new ArgumentException("Context switch tool should not have been inherited on commandline");
			}
			_log.Info("Test found fresh context switching tool");
			ContextEnroll();
		}

		/// <summary>Defer execution to another thread</summary>
		protected void ContextSwitch() {
			_log.Info("Initiating context switch");
			_cst.DoContextSwitch();
			_log.Info("We have received control again");
		}
		/// <summary>Release all control of context switching and rely on natural operating system switches</summary>
		protected void ContextNatural() {
			_log.Info("Unenrolling in cooperative multi-tasking");
			_cst.Unlock();
		}
		/// <summary>Begin participating in cooperative multi-tasking</summary>
		protected void ContextEnroll() {
			_log.Info("Enrolling in cooperative multi-tasking");
			_cst.Enroll();
			_log.Info("Enrollment complete");
		}
			
		public class TestLogger {
			private PipeTestWrapper _parent;
			public TestLogger(PipeTestWrapper parent) {
				_parent = parent;
			}
			public void Error(string msg) {
				Error(null, msg);
			}
			public void Error(string msg, params object[] format)
			{
				this.Error(string.Format(msg, format));
			}
			public void Error(Exception exc, string msg, params object[] format)
			{
				Error(exc, string.Format(msg, format));
			}
			public void Error(Exception exc, string msg)
			{
				WriteError(exc, msg);
				_parent._executionError = true;
			}
			public void Info(string msg) {
				WriteInfo(msg);
			}
			public void Info(string msg, params object[] format) {
				Info(string.Format(msg, format));
			}
			public void Test(string msg) {
				WriteTest(msg);
			}
			public void Test(string msg, params object[] format) {
				Test(string.Format(msg, format));
			}

			private const string INDENT = "    ";
			private string GetHeader(string type)
			{
				return string.Format("[{1}][{0}]", _parent.TestName, type);
			}
			private string Format(string msg) {
				return msg.Replace(Environment.NewLine, Environment.NewLine + INDENT);
			}
			private void Write(string header, string msg) {
				Console.WriteLine(header + msg.Replace(Environment.NewLine, Environment.NewLine + header));
			}
			private void WriteError(Exception exc, string msg)
			{
				lock (this) {
					bool indent = false;
					if (msg != null) {
						Write(GetHeader("ERROR"), Format(msg));
						indent = true;
					}
					if (exc != null) {
						var excMsg = Format(exc.ToString());
						if (indent) {
							excMsg = INDENT + excMsg;
						}
						Write(GetHeader("ERROR"), excMsg);
						var disposed = exc as ObjectDisposedException;
						if (disposed != null) {
							Write(GetHeader("ERROR"), INDENT + "Object disposed name: " + disposed.ObjectName);
						}
					}
				}
			}
			private void WriteInfo(string msg)
			{
				lock (this) {
					Write(GetHeader("INFO"), Format(msg));
				}
			}
			private void WriteTest(string msg)
			{
				lock (this) {
					Write(GetHeader("TEST"), Format(msg));
				}
			}
		}

		protected abstract void DoTest(string[] arguments);

		private void ExecuteInternal()
		{
			try
			{
				DoTest(_executionArgs);
			}
			catch (Exception eError)
			{
				_log.Error(eError, "Test failed with unhandled exception");
				_log.Error("Exception type: {0}", eError.GetType());
			}
		}

		public virtual string TestName
		{
			get 
			{
				return this.GetType().Name;
			}
		}
		public abstract string TestDescription
		{
			get;
		}

		public string FormatArguments(IEnumerable<string> clientArgs) {
			return FormatArguments(clientArgs.ToArray());
		}
		public string FormatArguments(params string[] clientArgs) {
			var result = string.Join(" ", clientArgs);

			_log.Info("Generated argument list:{0}{1}", Environment.NewLine, result);

			return result;
		}

		public int Execute(string[] arguments)
		{
			try 
			{
				_log.Info("Test {0} started at {1:O}", this.TestName, DateTime.Now);
				_log.Info("Working directory {0}", Environment.CurrentDirectory);

				foreach (var arg in arguments) {
					if (arg.StartsWith("/contextSwitch:")) {
						Guid csName;
						if (!Guid.TryParse(arg.Substring("/contextSwitch:".Length), out csName)) {
							_log.Error("Unable to parse /contextSwitch: argument as GUID");
							return 10;
						}
						_cst = new ContextSwitchTool(csName, true);
					}
				}
				if (_cst == null) {
					_cst = new ContextSwitchTool(Guid.NewGuid(), false);
				}

				this._executionArgs = arguments;
				Stopwatch runTimer = Stopwatch.StartNew();

				_log.Test("Starting testing thread");

				Thread testThread = new Thread(new ThreadStart(this.ExecuteInternal));
				testThread.Start();

				while (testThread.IsAlive)
				{
					if (runTimer.Elapsed > this._timeoutDuration) {
						_log.Error("Test timeout '{0}' has been exceeded, terminating", this._timeoutDuration);
						testThread.Abort();
						return 2;
					}

					_log.Info("Test runtime of {0}", runTimer.Elapsed);

					Thread.Sleep(1000);
				}
				runTimer.Stop();

				if (this._executionError) {
					_log.Error("Test failed in {0}", runTimer.Elapsed);
					return 1;
				}

				_log.Info("Test Succeeded in {0}", runTimer.Elapsed);
				return 0;
			}
			catch (Exception eError)
			{
				_log.Error(eError, "Test runner failed");
				return 3;
			}
			finally {
				if (_cst != null) { _cst.Dispose(); }
			}
		}

		public class ProcessLauncher
		{
			private TestLogger _log;
			private PipeTestWrapper _parent;
			private List<string> _arguments = new List<string>();
			private string _clientExe;
			private Process _process; 
			public readonly string ProcessSwitch;
			public readonly bool ParseFailure;

			public ProcessLauncher(PipeTestWrapper parent, string[] arguments)
				: this (parent, "/client:", arguments)
			{}

			public ProcessLauncher(PipeTestWrapper parent, string processSwitch, string[] arguments) {
				this._log = parent._log;
				this._parent = parent;
				this.ProcessSwitch = processSwitch;

				foreach (string arg in arguments) {
					if (arg.StartsWith(processSwitch)) {
						_clientExe = arg.Substring(processSwitch.Length);
					}
				}
				this.ParseFailure = _clientExe == null;

				AddArgument("/contextSwitch:", _parent._cst.Name);
			}

			public void AddArgument(string sw, string format, params object[] args) {
				AddArgument(sw + string.Format(format, args));
			}
			public void AddArgument(string sw, string data) {
				AddArgument(sw + data);
			}
			public void AddArgument(string arg) {
				if (_process != null) {
					throw new InvalidOperationException("process already started");
				}
				_arguments.Add(arg); 
			}

			public void Launch() {
				if (_process != null) {
					throw new InvalidOperationException("Process already started");
				}
				if (_clientExe == null) {
					throw new InvalidOperationException("Parse failure while trying to get client exe");
				}

				var args = _parent.FormatArguments(_arguments);
				_log.Test("Starting process '{0}'", _clientExe, args);
				_process = new Process();
				_process.StartInfo.FileName = _clientExe;
				_process.StartInfo.WorkingDirectory = Environment.CurrentDirectory;
				_process.StartInfo.Arguments = args;
				_process.StartInfo.UseShellExecute = false;
				_process.Start();
				_log.Test("Done starting process '{0}'", _clientExe);
			}

			public void WaitForExit() {
				if (_process == null) {
					throw new InvalidOperationException("Process not yet started");
				}
				_log.Test("Awaiting process '{0}' exit", _clientExe);
				_process.WaitForExit();
				_log.Test("Done awaiting process '{0}' exit", _clientExe);
			}
		}
		/// <summary>
		/// Similar to StreamReader class but with no buffering
		/// </summary>
		public class PipeReader : IDisposable {
			private Stream _stream;
			private Decoder _decoder;
			private byte[] _minBuffer = new byte[8];
			private int _offset = 0;
			public PipeReader(Stream stream) {
				this._stream = stream;
				this._decoder = Encoding.UTF8.GetDecoder();
			}
			public char ReadChar() {
				if (_offset != 0) { throw new InvalidOperationException("offset indicates illegal state"); }
				_decoder.Reset();
				do {
					if (_stream.Read(_minBuffer, _offset, 1)==1) {
						++_offset;
					} else {
						_offset = 0;
						throw new InvalidOperationException("Stream closed");
					}
				} while (_decoder.GetCharCount(_minBuffer, 0, _offset) <= 0); 
				char[] result = new char[1];
				if (_decoder.GetChars(_minBuffer, 0, _offset, result, 0)==1) {
					_offset = 0;
					return result[0];
				} else {
					_offset = 0;
					throw new InvalidOperationException("Unexpected number of chars read from stream");
				}
			}
			public string ReadLine() {
				StringBuilder result = new StringBuilder();
				bool gotReturn = false;
				while (true) {
					char next = this.ReadChar();
					if (next == '\r' && !gotReturn) { gotReturn = true; }
					else if (next == '\r') { result.Append(next); }
					else if (gotReturn && next == '\n') { break; }
					else { result.Append(next); gotReturn = false; }
				}
				return result.ToString();
			}
			public void Dispose() {
				// dont actually dispose anything
			}
		}
		public class PipeWriter : IDisposable {
			private Stream _stream;
			private Encoder _encoder;
			private readonly int _newlineLen;
			public PipeWriter(Stream stream) {
				this._stream = stream;
				this._encoder = Encoding.UTF8.GetEncoder();
				_newlineLen = this._encoder.GetByteCount(PipeWriter.NewLine, 0, PipeWriter.NewLine.Length, true);
			}
			private static readonly char[] NewLine = new char[] { '\r', '\n' };

			public void WriteLine(string s) {
				char[] input = s.ToCharArray();
				byte[] output = new byte[_encoder.GetByteCount(input, 0, input.Length, true) + _newlineLen];
				int offset = _encoder.GetBytes(input, 0, input.Length, output, 0, true);
				offset += _encoder.GetBytes(PipeWriter.NewLine, 0, PipeWriter.NewLine.Length, output, offset, true);
				if (offset != output.Length) {
					throw new InvalidOperationException("Incorrect offset while writing");
				}
				_stream.Write(output, 0, output.Length);
				_stream.Flush();
			}

			public void Dispose() {
				// dont actually dispose anything
			}
		}
		/// <summary>Allows threads to enroll in a cooperative multi-tasking scheme for easy IPC synchronization</summary>
		public class ContextSwitchTool : IDisposable
		{
			private Mutex _sync;
			private string _name;
			private bool _inherited;
			private bool _ownsMutex = false;
			public ContextSwitchTool(Guid name, bool inherited) {
				_inherited = inherited;
				_sync = new Mutex(false, _name = name.ToString("N"));
			}
			public void Dispose() {
				Unlock();
			}
			public void DoContextSwitch() {
				Unlock();
				Lock();
			}
			public void Unlock() {
				if (_ownsMutex) {
					if (File.Exists(".mutex." + _name)) {
						File.Delete(".mutex." + _name);
					}
					_ownsMutex = false;
				}
				Thread.Sleep(100);
			}
			private void Lock() {
				if (!_ownsMutex) {
					while (File.Exists(".mutex." + _name)) { Thread.Sleep(10); }
					File.Create(".mutex." + _name).Dispose();
				}
				_ownsMutex = true;
			}
			/// <summary>Enroll in cooperative multi-tasking and wait for another thread to defer to us</summary>
			public void Enroll() {
				Lock();
			}
			public string Name { get { return _name; } }
			public bool Inherited { get { return _inherited; } }
		}
	}
}
