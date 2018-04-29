using System;
using QJInterface;
// ReSharper disable UnusedMember.Global

namespace QJExternalTool
{
	public class ExternalTool : IExternalTool
	{
		// This function is call by QJTrader only one time.
		public void InitializeTool(IHost host) => new TestTool(host).Show();

		public void Closing() => Console.WriteLine(@"**** Closing() ****");
	}
}
