using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using MultiThreadedDownloaderLib;
using static TwitchApiLib.TwitchApi;
using static TwitchApiLib.Utils;

namespace TwitchApiLib
{
	public class TwitchVodPage
	{
		public List<TwitchVodResult> VodList { get; private set; }
		private string _nextPageToken = null;
		public string RawData { get; }

		public TwitchVodPage(string rawData)
		{
			RawData = rawData;
		}

		public static TwitchVodPageResult Get(ulong userId, uint maxVideos, string pageToken = null)
		{
			TwitchApplication application = GetApplication();
			int errorCode = GetHelixOauthToken(application, out string token);
			if (errorCode == 200)
			{
				string url = GenerateChannelVideosRequestUrl(userId.ToString(), maxVideos, pageToken);
				FileDownloader d = MakeTwitchApiBearerClient(application.ClientId, token);
				d.Url = url;
				errorCode = d.DownloadString(out string response);
				d.Dispose();

				if (errorCode == 200)
				{
					return new TwitchVodPageResult(new TwitchVodPage(response), errorCode);
				}
			}
			return new TwitchVodPageResult(null, errorCode);
		}

		public string GetNextPageToken()
		{
			if (_nextPageToken == null)
			{
				JObject json = TryParseJson(RawData);
				if (json != null)
				{
					JToken jt = json.Value<JToken>("pagination");
					if (jt == null || !jt.HasValues)
					{
						_nextPageToken = string.Empty;
						return string.Empty;
					}

					jt = jt.Value<JObject>().Value<JToken>("cursor");
					if (jt == null)
					{
						_nextPageToken = string.Empty;
						return string.Empty;
					}

					_nextPageToken = jt.Value<string>();
				}
				else
				{
					_nextPageToken = string.Empty;
				}
			}

			return _nextPageToken;
		}

		public int Parse()
		{
			JObject json = TryParseJson(RawData);
			if (json != null)
			{
				VodList = new List<TwitchVodResult>();
				JArray jaData = json.Value<JArray>("data");
				if (jaData != null && jaData.Count > 0)
				{
					foreach (JObject jVod in jaData.Cast<JObject>())
					{
						TwitchVodResult vodResult = ParseVodInfo(jVod);
						VodList.Add(vodResult);
					}
				}

				return VodList.Count;
			}

			return 0;
		}
	}
}
