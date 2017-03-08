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
	/// using(var l = new TryLockOrSkip(someLock))  //if lock is free take it otherwize skip whole block
	/// {
	///		if(l.Locked)
	///     {
	///												//code to execute while keeping lock...
	///     }                                   
	/// }                                           //unlock
	/// 
	/// </summary>
    public class TryLockOrSkip:IDisposable
	{
		private readonly object _lock;

		public TryLockOrSkip(object lockObject = null)
		{
			if (lockObject == null)
			{
				lockObject = new object();
			}

			if (System.Threading.Monitor.TryEnter(lockObject))
			{
				this._lock = lockObject;                                            //took lock here
			}
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);       
		}

		bool _disposed = false;

		protected virtual void Dispose(bool disposing)
		{
			if (!this._disposed)
			{
				if (disposing)
				{
					// Free any managed objects here. 
					//
					if (this._lock != null)
					{
						System.Threading.Monitor.Exit(_lock);                       //release lock here
					}
				}

				// Free any unmanaged objects here. 
				//


				this._disposed = true;
			}
		}

		~TryLockOrSkip()
		{
			 this.Dispose(false);
		}
	}
}
