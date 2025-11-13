
namespace TwitchApiLib
{
	public class TwitchUserResult
	{
		public TwitchUser User { get; }
		public int ErrorCode { get; }
		public string ErrorMessage { get; }
		public string RawUserData { get; }

		public TwitchUserResult(TwitchUser twitchUser, int errorCode, string errorMessage, string rawUserData)
		{
			User = twitchUser;
			ErrorCode = errorCode;
			ErrorMessage = errorMessage;
			RawUserData = rawUserData;
		}
	}
}
