using Newtonsoft.Json.Linq;

namespace TwitchApiLib
{
	public class TwitchVideoPlaybackAccessToken : ITwitchPlaybackAccessToken
	{
		public string RawData { get; }
		public int ErrorCode { get; }

		public TwitchVideoPlaybackAccessToken(string rawData, int errorCode)
		{
			RawData = rawData;
			ErrorCode = errorCode;
		}

		public JObject GetToken(out string errorMessage)
		{
			JObject json = Utils.TryParseJson(RawData, out errorMessage);
			JToken jt = json?.Value<JObject>("data");
			if (jt == null)
			{
				errorMessage = json?.ToString();
				return null;
			}

			return jt.Value<JObject>("videoPlaybackAccessToken");
		}

		public JObject GetToken()
		{
			return GetToken(out _);
		}
	}
}
