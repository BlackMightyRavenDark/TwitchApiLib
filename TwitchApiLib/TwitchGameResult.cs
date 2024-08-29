
namespace TwitchApiLib
{
	public class TwitchGameResult
	{
		public TwitchGame Game { get; }
		public int ErrorCode { get; }

		public TwitchGameResult(TwitchGame game, int errorCode)
		{
			Game = game;
			ErrorCode = errorCode;
		}
	}
}
