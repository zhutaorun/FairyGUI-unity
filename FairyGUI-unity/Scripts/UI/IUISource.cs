using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FairyGUI
{
	public delegate void UILoadCallback();

	public interface IUISource
	{
		string fileName { get; set; }
		bool loaded { get; }
		void Load(UILoadCallback callback);
	}
}
