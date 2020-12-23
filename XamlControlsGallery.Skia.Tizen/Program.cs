using Tizen.Applications;
using Uno.UI.Runtime.Skia;

namespace XamlControlsGallery.Skia.Tizen
{
	class Program
	{
		static void Main(string[] args)
		{
			var host = new TizenHost(() => new XamlControlsGallery.App(), args);
			host.Run();
		}
	}
}
