using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CraftSynth.BuildingBlocks.Common.Patterns
{

	/// <summary>
	/// Instead using:--------------------------------------------
	/// 
	/// 
	/// private static object _lock = new object();
	///  
	/// if(Monitor.TryEnter(_lock))
	/// {
	///		try
	///		{
	///			...
	///			...
	///		}
	///		finally
	///		{
	///			Monitor.Exit(_lock);
	///		}
	/// }
	/// 
	/// 
	/// you can use:----------------------------------------------
	/// 
	/// 
	/// using(var a = new NonparallelExecution("someLockObject"))
	/// {
	///		if(a.IsNotExecuting)
	///		{
	///			...
	///			...
	///		}
	/// }
	/// </summary>
	public class NonparallelExecution:IDisposable
	{
		#region Private Members
		private static object _lock = new object();
		private static Dictionary<string, bool> _codePartNamesAndIsExecutingState = new Dictionary<string, bool>();

		private string _currentCodePartName;
		#endregion

		#region Properties
		public bool IsNotExecuting
		{
			get
			{
				lock (_lock)
				{
					return !_codePartNamesAndIsExecutingState[_currentCodePartName];
				}
			}
		}
		#endregion

		#region Public Methods
		#endregion

		#region Constructors And Initialization

		public NonparallelExecution(string codePartName)
		{
			lock (_lock)
			{
				this._currentCodePartName = codePartName;
				if (_codePartNamesAndIsExecutingState.ContainsKey(codePartName))
				{
					_codePartNamesAndIsExecutingState[codePartName] = true;
				}
				else
				{
					_codePartNamesAndIsExecutingState.Add(codePartName, true);
				}
			}
		}
		#endregion

		#region Deinitialization And Destructors
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);       
		}

		bool _disposed = false;

		protected virtual void Dispose(bool disposing)
		{
			lock (_lock)
			{
				if (!this._disposed)
				{

					if (disposing)
					{
						// Free any managed objects here. 
						//
					}

					// Free any unmanaged objects here. 
					//

					try
					{
						if (_codePartNamesAndIsExecutingState[_currentCodePartName] == false)
						{
							throw new Exception("Invalid usage of NonparallelExecution class. Please see class description.");
						}
						else
						{
							_codePartNamesAndIsExecutingState[_currentCodePartName] = false;
						}
					}
					catch (KeyNotFoundException)
					{
						throw new Exception("Invalid usage of NonparallelExecution class. Please see class description.");
					}
					
					this._disposed = true;
				}
			}
		}

		~NonparallelExecution()
		{
			 this.Dispose(false);
		}
		#endregion

		#region Event Handlers
		#endregion

		#region Private Methods
		#endregion

		#region Helpers
		#endregion
	}
}
