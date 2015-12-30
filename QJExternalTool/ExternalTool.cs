using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QJInterface;

namespace QJExternalTool
{
	public class ExternalTool : QJInterface.IExternalTool
	{
		#region Variables
		IHost m_host;
		TestTool m_testTool;
		#endregion

		#region IExternalTool Members
		// This function is call by QJTrader only one time.
		public void InitializeTool(IHost host)
		{
			m_host = host;
			m_testTool = new TestTool(host);
			m_testTool.Show();
		}

		public void Closing()
		{
			Console.WriteLine("**** Closing() ****");
		}
		#endregion
	}
}
