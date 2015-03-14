#region Using Statements

using System.Linq;
using System.Xml.Linq;

#endregion

namespace SharpComicVine.Utils
{
	public static class XmlUtilFunctions
	{
		public static string GetNodeValue(XDocument xDocument, string name)
		{
			var result = string.Empty;

			var elements = xDocument.Descendants(name).ToList();

			if (elements.Any())
			{
				result = elements.First().Value;
			}

			return result;
		}
	}
}