using System;
using QJInterface;

namespace QJExternalTool
{
	public class ExternalTool : IExternalTool
	{
		#region Variables

	    private TestTool _testTool;
		#endregion

		#region IExternalTool Members
		// This function is call by QJTrader only one time.
		public void InitializeTool(IHost host)
		{
		    _testTool = new TestTool(host);
			_testTool.Show();
		}

		public void Closing()
		{
			Console.WriteLine("**** Closing() ****");
		}
		#endregion
	}
}
