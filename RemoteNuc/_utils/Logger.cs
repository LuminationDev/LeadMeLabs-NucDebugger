using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// A class responsbile for logging station addresses, messages, actions and responses that 
/// pass through the server.
/// </summary>
public static class Logger
{
	private static Queue<string> logQueue = new Queue<string>();
	
	public static void WriteLog<T>(T logMessage, bool writeToLogFile = true)
	{
		if (logMessage == null) return;
		string msg = $"[{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")}]: {logMessage.ToString()}";
		if (writeToLogFile)
		{
			lock (logQueue)
			{
				logQueue.Enqueue(msg);
			}
		}
		Console.WriteLine(msg);
	}

	public static void WorkQueue()
	{
		using (StreamWriter w = File.AppendText("_logs/"+ DateTime.Now.ToString("yyyy_MM_dd") + "_log.txt"))
		{
			Queue<string> clonedQueue;
			lock (logQueue)
			{
				clonedQueue = new Queue<string>(logQueue);
				logQueue = new Queue<string>();
			}
			while (clonedQueue.Count > 0)
			{
				w.WriteLine(clonedQueue.Dequeue());
			}
		}
	}
}
