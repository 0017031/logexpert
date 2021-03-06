using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace LogExpert
{
	public class FilterPipe
	{
		#region Fields

		private IList<int> _lastLinesHistoryList = new List<int>();
		private StreamWriter _writer;
		private IList<int> _lineMappingList = new List<int>();
		private static readonly NLog.ILogger _logger = NLog.LogManager.GetCurrentClassLogger();

		#endregion Fields

		#region cTor

		public FilterPipe(FilterParams filterParams, LogWindow logWindow)
		{
			FilterParams = filterParams;
			LogWindow = logWindow;
			IsStopped = false;
			FileName = Path.GetTempFileName();

			_logger.Info("Created temp file: " + FileName);
		}

		#endregion cTor

		#region Event

		public delegate void ClosedEventHandler(FilterPipe sender);

		public event ClosedEventHandler Closed;

		#endregion Event

		#region Properties

		// the parent LogWindow
		// own window
		public bool IsStopped { get; set; }

		public void OpenFile()
		{
			FileStream fStream = new FileStream(FileName, FileMode.Append, FileAccess.Write, FileShare.Read);
			_writer = new StreamWriter(fStream, new UnicodeEncoding(false, false));
		}

		public LogWindow LogWindow { get; private set; }

		public LogWindow OwnLogWindow { get; set; }

		public string FileName { get; private set; }

		public FilterParams FilterParams { get; private set; }

		public IList<int> LastLinesHistoryList
		{
			get
			{
				return _lastLinesHistoryList;
			}
		}

		#endregion Properties

		#region Public Methods

		public void CloseFile()
		{
			if (_writer != null)
			{
				_writer.Close();
				_writer.Dispose();
				_writer = null;
			}
		}

		public bool WriteToPipe(string textLine, int orgLineNum)
		{
			try
			{
				lock (FileName)
					lock (_lineMappingList)
					{
						try
						{
							_writer.WriteLine(textLine);
							_lineMappingList.Add(orgLineNum);
							return true;
						}
						catch (IOException e)
						{
							_logger.Error(e, "writeToPipe(): ");
							return false;
						}
					}
			}
			catch (IOException ex)
			{
				_logger.Error(ex, "writeToPipe(): file was closed: " + FileName);
				return false;
			}
		}

		public int GetOriginalLineNum(int lineNum)
		{
			lock (_lineMappingList)
			{
				if (_lineMappingList.Count > lineNum)
				{
					return _lineMappingList[lineNum];
				}
				else
				{
					return -1;
				}
			}
		}

		public void ShiftLineNums(int offset)
		{
			_logger.Debug("FilterPipe.ShiftLineNums() offset=" + offset);
			List<int> newList = new List<int>();
			lock (_lineMappingList)
			{
				foreach (int lineNum in _lineMappingList)
				{
					int line = lineNum - offset;
					if (line >= 0)
					{
						newList.Add(line);
					}
					else
					{
						newList.Add(-1);
					}
				}
				_lineMappingList = newList;
			}
		}

		public void ClearLineNums()
		{
			_logger.Debug("FilterPipe.ClearLineNums()");
			lock (_lineMappingList)
			{
				for (int i = 0; i < _lineMappingList.Count; ++i)
				{
					_lineMappingList[i] = -1;
				}
			}
		}

		public void ClearLineList()
		{
			lock (_lineMappingList)
			{
				_lineMappingList.Clear();
			}
		}

		public void RecreateTempFile()
		{
			lock (_lineMappingList)
			{
				_lineMappingList = new List<int>();
			}
			lock (FileName)
			{
				CloseFile();
				// trunc file

				using (FileStream fStream = new FileStream(FileName, FileMode.Truncate, FileAccess.Write, FileShare.Read))
				{
					fStream.SetLength(0);
					fStream.Close();
				}
			}
		}

		public void CloseAndDisconnect()
		{
			ClearLineList();
			OnClosed();
		}

		#endregion Public Methods

		#region Private Methods

		private void OnClosed()
		{
			if (Closed != null)
			{
				Closed(this);
			}
		}

		#endregion Private Methods
	}
}