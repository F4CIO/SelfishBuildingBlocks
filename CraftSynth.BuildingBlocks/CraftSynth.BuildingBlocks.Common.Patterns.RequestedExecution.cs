using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace CraftSynth.BuildingBlocks.Common.Patterns
{
	/// <summary>
	/// Suitable for doing some slow update. It will execute update until there are no pending requests. 
	/// Even if somebody request update during updating new update will be done on finish to cover that request. 
	/// That way some state can always be kept up to date.
	/// </summary>
	public class RequestedExecution
	{
		#region Private Members
		private Action _action;
		private static object _lock = new object();
		private int _requests = 0;
		#endregion

		#region Properties
		#endregion

		#region Public Methods
		public void RequestExecution()
		{
			_requests++;
		}
		
		public void ExecuteOrRequestExecution(bool ifAlreadyExecutingJustMakeRequest, bool ifStartedExecutionAndAnotherWasRequestedInMeantimeRepeatAtEnd, bool executeEvenIfNoRequested = true)
		{					
				if(!Monitor.TryEnter(_lock))
				{
					if(ifAlreadyExecutingJustMakeRequest)
					{
						_requests++;
					}
				}
				else
				{
					try
					{
						if (executeEvenIfNoRequested)
						{
							_requests++;
						}
						if(!ifStartedExecutionAndAnotherWasRequestedInMeantimeRepeatAtEnd)
						{
							if(_requests>0)
							{ 
								_requests = 0;
				 
								//do updating	
								_action.Invoke();		
							}
						}
						else
						{
							while(_requests>0)
							{ 
								_requests = 0;
				 
								//do updating	
								_action.Invoke();		
							}
						}
					}
					finally
					{

						Monitor.Exit(_lock);
					}
				}
		
		}
		public void ExecuteIfAnyRequested(bool requestAnotherIfExecuting, bool repeatExecutionIfAnyRequestedWhileExecuting)
		{
			this.ExecuteOrRequestExecution(requestAnotherIfExecuting, repeatExecutionIfAnyRequestedWhileExecuting, false);
		}
		#endregion

		#region Constructors And Initialization

		public RequestedExecution(Action action)
		{
			this._action = action;
		}
		#endregion

		#region Deinitialization And Destructors

		#endregion

		#region Event Handlers

		#endregion

		#region Private Methods

		#endregion

		#region Helpers

		#endregion
	}
}
