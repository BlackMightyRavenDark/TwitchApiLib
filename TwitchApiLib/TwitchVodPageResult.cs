
namespace TwitchApiLib
{
	public class TwitchVodPageResult
	{
		public TwitchVodPage VodPage { get; }
		public int ErrorCode { get; }

		public TwitchVodPageResult(TwitchVodPage vodPage, int errorCode)
		{
			VodPage = vodPage;
			ErrorCode = errorCode;
		}
	}
}
