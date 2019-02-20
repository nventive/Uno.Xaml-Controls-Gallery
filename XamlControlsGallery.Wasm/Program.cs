using System;
using System.Linq;
using Uno.Foundation;
using Uno.UI;
using Windows.UI.Xaml;

namespace AppUIBasics.Wasm
{
	public class Program
	{
		static int Main(string[] args)
        {
            WebAssemblyRuntime.InvokeJS("Uno.UI.Demo.Analytics.reportPageView('main');");

            Windows.UI.Xaml.Application.Start(_ => new App());

            return 0;
        }
    }
}
