using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

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
			return Utils.GetChannelVideosPage(userId.ToString(), maxVideos, pageToken);
		}

		public string GetNextPageToken()
		{
			if (_nextPageToken == null)
			{
				JObject json = Utils.TryParseJson(RawData);
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
			JObject json = Utils.TryParseJson(RawData);
			if (json != null)
			{
				VodList = new List<TwitchVodResult>();
				JArray jaData = json.Value<JArray>("data");
				if (jaData != null && jaData.Count > 0)
				{
					foreach (JObject jVod in jaData.Cast<JObject>())
					{
						TwitchVodResult vodResult = Utils.ParseVodInfo(jVod);
						VodList.Add(vodResult);
					}
				}

				return VodList.Count;
			}

			return 0;
		}
	}
}
