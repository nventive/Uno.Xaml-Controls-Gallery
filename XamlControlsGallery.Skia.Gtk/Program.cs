using GLib;
using System;
using System.Collections;
using Uno.Extensions;
using Uno.Foundation.Extensibility;

namespace AppUIBasics.Wasm
{
	class Program
	{
		static void Main(string[] args)
		{
			ExceptionManager.UnhandledException += delegate (UnhandledExceptionArgs expArgs)
			{
				Console.WriteLine("GLIB UNHANDLED EXCEPTION" + expArgs.ExceptionObject.ToString());
				expArgs.ExitApplication = true;
			};

			var host = new Uno.UI.Runtime.Skia.GtkHost(() => new App(), args);

			host.Run();
		}
	}
}
