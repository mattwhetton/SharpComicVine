#region Using Statements

using System.IO;
using System.Net;
using System.Threading.Tasks;
using SharpComicVine.Models;

#endregion

namespace SharpComicVine
{
	internal static class ComicVineConnection
	{
		public static async Task<ComicVineResponse> ConnectAndRequest(string query)
		{
			var comicVineResponse = new ComicVineResponse();

			var httpWebRequest = (HttpWebRequest) WebRequest.Create(query);

			using (var webResponse = await httpWebRequest.GetResponseAsync())
			using (var streamReader = new StreamReader(webResponse.GetResponseStream()))
			{
				comicVineResponse.Status = "OK";
				comicVineResponse.Response = streamReader.ReadToEnd();
			}

			return comicVineResponse;
		}
	}
}