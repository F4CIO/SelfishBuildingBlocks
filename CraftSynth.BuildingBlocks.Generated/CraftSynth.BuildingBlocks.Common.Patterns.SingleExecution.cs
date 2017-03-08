using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace CraftSynth.BuildingBlocks.Common.Patterns
{
	/// <summary>
	/// Insures that code is executed just once
	/// Usage example:
	/// 
	/// if(SingleExecution.IsSafeToExecuteOnce("codePartName"))
	/// {
	///		
	/// }
	/// </summary>
	public class SingleExecution
	{
		private static object _lock = new object();
		private static HashSet<string> _codeExecuted = new HashSet<string>();
	
		public static bool IsSafeToExecuteOnce(string codePartName)
		{
			lock(_lock)
			{
				if (!_codeExecuted.Contains(codePartName))
				{
					_codeExecuted.Add(codePartName);
					return true;
				}
				else
				{
					return false;
				}
			}
		}

		public static void Reset(string codePartName)
		{
			lock (_lock)
			{
				if (_codeExecuted.Contains(codePartName))
				{
					_codeExecuted.Remove(codePartName);
				}
			}
		}
	}
}
