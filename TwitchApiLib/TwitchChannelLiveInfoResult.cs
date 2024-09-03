
namespace TwitchApiLib
{
	public class TwitchChannelLiveInfoResult
	{
		public TwitchChannelLiveInfo LiveInfo { get; }
		public int ErrorCode { get; }

		public TwitchChannelLiveInfoResult(TwitchChannelLiveInfo liveInfo, int errorCode)
		{
			LiveInfo = liveInfo;
			ErrorCode = errorCode;
		}
	}
}
