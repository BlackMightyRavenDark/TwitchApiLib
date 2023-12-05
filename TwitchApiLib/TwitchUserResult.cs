
namespace TwitchApiLib
{
    public class TwitchUserResult
    {
        public TwitchUser User { get; }
        public int ErrorCode { get; }

        public TwitchUserResult(TwitchUser twitchUser, int errorCode)
        {
            User = twitchUser;
            ErrorCode = errorCode;
        }
    }
}
