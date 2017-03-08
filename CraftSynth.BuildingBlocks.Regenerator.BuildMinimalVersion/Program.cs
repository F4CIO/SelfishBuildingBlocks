using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CraftSynth.BuildingBlocks.Regenerator.BuildMinimalVersion
{
	class Program
	{
		/// <summary>
		/// http://www.f4cio.com/SelfishBuildingBlocks.aspx
		/// </summary>
		/// <param name="args">The arguments.</param>
		static void Main(string[] args)
		{
			var r = new Regenerator();
			r.BuildMinimalVersion();
		}
	}
}
