using Newtonsoft.Json.Linq;

namespace TwitchApiLib
{
	public interface ITwitchPlaybackAccessToken
	{
		string RawData { get; }
		int ErrorCode { get; }

		JObject GetToken(out string errorMessage);
		JObject GetToken();
	}
}
