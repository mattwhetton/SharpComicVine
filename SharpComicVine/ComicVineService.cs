#region Using Statements

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharpComicVine.Models;
using SharpComicVine.Utils;

#endregion

namespace SharpComicVine
{
	public class ComicVineService
	{
		public ComicVineService()
		{
			Initialize();
		}

		public string ComicVineKey { get; set; }

		public MatchType MatchType { get; set; }

		public SearchType SearchType { get; set; }

		public string SearchAddress { get; private set; }

		public string ComicVineAddress { get; private set; }


		private void Initialize()
		{
			SearchType = SearchType.Xml;
			MatchType = MatchType.AbsoluteMatch;
			SearchAddress = "http://api.comicvine.com/search/";
			ComicVineAddress = "http://api.comicvine.com/";
		}

		private List<ComicVineVolume> FindVolumeIdByName(string volumeName)
		{
			string query = null;

			if (SearchType == SearchType.Xml)
			{
				query = ComicVineAddress + "volumes/?api_key=" + ComicVineKey +
				        "&format=xml&field_list=id,name,publisher&filter=name:" + volumeName;
			}
			else
			{
				query = ComicVineAddress + "volumes/?api_key=" + ComicVineKey +
				        "&format=json&field_list=id,name,publisher&filter=name:" + volumeName;
			}

			var comicVineResponse = ComicVineConnection.ConnectAndRequest(query);

			var comicVineVolumeLists = new ConcurrentBag<List<ComicVineVolume>>();

			if (comicVineResponse.Result.Status == "OK")
			{
				var firstData = ComicVineReader.GetFirstVolumeQueryResponse(SearchType, comicVineResponse.Result.Response);

				if (firstData.number_of_total_results > 0)
				{
					var parallelThreads = SystemEnvironment.ProcessorCountOptimizedForEnvironment();

					var numberOfIterations = (int) Math.Ceiling(((double) firstData.number_of_total_results/(double) firstData.limit));

					Parallel.For(0, numberOfIterations, new ParallelOptions() {MaxDegreeOfParallelism = parallelThreads}, i =>
					{
						var offset = i*firstData.limit;
						var secondQuery = query + "&offset=" + offset.ToString();

						var secondResponse = ComicVineConnection.ConnectAndRequest(secondQuery);
						var volumeList = ComicVineReader.GetVolumeQueryResponse(SearchType, secondResponse.Result.Response);

						comicVineVolumeLists.Add(volumeList);
						secondResponse = null;
					});
				}
			}

			if (MatchType == MatchType.AbsoluteMatch)
			{
				var filteredComicVineVolumeLists = new ConcurrentBag<List<ComicVineVolume>>();
				var filteredComicVineVolumeList = new List<ComicVineVolume>();

				foreach (var volumeList in comicVineVolumeLists)
				{
					foreach (var volume in volumeList)
					{
						if (volume.name == volumeName)
						{
							filteredComicVineVolumeList.Add(volume);
						}
					}
				}

				filteredComicVineVolumeLists.Add(filteredComicVineVolumeList);
				comicVineVolumeLists = filteredComicVineVolumeLists;
			}

			var comicVineVolumeList = new List<ComicVineVolume>();

			foreach (var comicVineVolume in comicVineVolumeLists)
			{
				comicVineVolumeList.AddRange(comicVineVolume);
			}

			return comicVineVolumeList;
		}

		public List<ComicVineVolume> SearchVolume(string volumeName)
		{
			var comicVineVolumeList = FindVolumeIdByName(volumeName);

			var comicVineVolumeBag = new ConcurrentBag<ComicVineVolume>();

			var parallelThreads = SystemEnvironment.ProcessorCountOptimizedForEnvironment();

			Parallel.ForEach(comicVineVolumeList, new ParallelOptions() {MaxDegreeOfParallelism = parallelThreads},
				comicVineVolume =>
				{
					try
					{
						if (comicVineVolume != null)
						{
							comicVineVolumeBag.Add(GetComicVineVolume(comicVineVolume.id));
						}
					}
					catch (AggregateException aggregateException)
					{
						foreach (var exception in aggregateException.InnerExceptions)
						{
							if (exception is ArgumentException)
							{
								// Don't act on this
							}
							else
							{
								throw exception;
							}
						}
					}
				});

			return comicVineVolumeBag.ToList();
		}

		public List<ComicVineIssue> SearchIssue(string volumeName, int issueNumber)
		{
			var comicVineVolumeList = FindVolumeIdByName(volumeName);

			var comicVineIssueBag = new ConcurrentBag<ComicVineIssue>();

			var parallelThreads = SystemEnvironment.ProcessorCountOptimizedForEnvironment();

			Parallel.ForEach(comicVineVolumeList, new ParallelOptions() {MaxDegreeOfParallelism = parallelThreads},
				comicVineVolume =>
				{
					try
					{
						if (comicVineVolume != null)
						{
							var detailedComicVineIssue = GetComicVineIssue(comicVineVolume.id, issueNumber);

							if (detailedComicVineIssue.issue_number == issueNumber.ToString())
							{
								comicVineIssueBag.Add(detailedComicVineIssue);
							}
						}
					}
					catch (AggregateException aggregateException)
					{
						foreach (var exception in aggregateException.InnerExceptions)
						{
							if (exception is ArgumentException)
							{
								// Don't act on this
							}
							else
							{
								throw exception;
							}
						}
					}
				});

			return comicVineIssueBag.ToList();
		}

		public ComicVineVolume GetComicVineVolume(int volumeId)
		{
			var detailedComicVineVolume = new ComicVineVolume();

			string query = null;

			if (SearchType == SearchType.Xml)
			{
				query = ComicVineAddress + "volume/4050-" + volumeId.ToString() + "/?api_key=" + ComicVineKey +
				        "&format=xml&field_list=id,api_detail_url,count_of_issues,description,image,name,publisher,start_year";
			}
			else
			{
				query = ComicVineAddress + "volume/4050-" + volumeId.ToString() + "/?api_key=" + ComicVineKey +
				        "&format=json&field_list=id,api_detail_url,count_of_issues,description,image,name,publisher,start_year";
			}

			var firstResponse = ComicVineConnection.ConnectAndRequest(query);

			detailedComicVineVolume = ComicVineReader.GetVolume(SearchType, firstResponse.Result.Response);

			return detailedComicVineVolume;
		}

		public ComicVineIssue GetComicVineIssue(int issueId)
		{
			var comicVineIssue = new ComicVineIssue();

			string query = null;

			if (SearchType == SearchType.Xml)
			{
				query = ComicVineAddress + "issue/4000-" + issueId.ToString() + "/?api_key=" + ComicVineKey +
				        "&format=xml&field_list=id,api_detail_url,description,image,issue_number,name,person_credits,character_credits,cover_date,volume";
			}
			else
			{
				query = ComicVineAddress + "issue/4000-" + issueId.ToString() + "/?api_key=" + ComicVineKey +
				        "&format=json&field_list=id,api_detail_url,description,image,issue_number,name,person_credits,character_credits,cover_date,volume";
			}

			var firstResponse = ComicVineConnection.ConnectAndRequest(query);

			comicVineIssue = ComicVineReader.GetIssue(SearchType, firstResponse.Result.Response, true);


			return comicVineIssue;
		}

		public ComicVineIssue GetComicVineIssue(int volumeId, int issueNumber)
		{
			var comicVineIssue = new ComicVineIssue();

			string query = null;

			if (SearchType == SearchType.Xml)
			{
				query = ComicVineAddress + "issues/?api_key=" + ComicVineKey +
				        "&format=xml&field_list=id,api_detail_url,issue_number,cover_date,name,image,person_credits,character_credits,volume&filter=issue_number:" +
				        issueNumber.ToString() + ",volume:" + volumeId.ToString();
			}
			else
			{
				query = ComicVineAddress + "issues/?api_key=" + ComicVineKey +
				        "&format=json&field_list=id,api_detail_url,issue_number,cover_date,name,image,person_credits,character_credits,volume&filter=issue_number:" +
				        issueNumber.ToString() + ",volume:" + volumeId.ToString();
			}

			var firstResponse = ComicVineConnection.ConnectAndRequest(query);

			comicVineIssue = ComicVineReader.GetIssue(SearchType, firstResponse.Result.Response, issueNumber, false);

			if (comicVineIssue.id > 0)
			{
				return GetComicVineIssue(comicVineIssue.id);
			}
			else
			{
				return comicVineIssue;
			}
		}
	}

	public enum MatchType
	{
		AbsoluteMatch,
		PartialMatch
	}

	public enum SearchType
	{
		Json,
		Xml
	}
}