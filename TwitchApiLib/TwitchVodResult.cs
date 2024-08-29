
namespace TwitchApiLib
{
	public class TwitchVodResult
	{
		public TwitchVod Vod { get; }
		public int ErrorCode { get; }
		public string ErrorMessage { get; }
		public string RawVodData { get; }

		public TwitchVodResult(TwitchVod vod, int errorCode, string errorMessage, string rawVodData)
		{
			Vod = vod;
			ErrorCode = errorCode;
			ErrorMessage = errorMessage;
			RawVodData = rawVodData;
		}
	}
}
