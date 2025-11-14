
namespace TwitchApiLib
{
	public class TwitchVodPlaylistResult
	{
		public TwitchVodPlaylist Playlist { get; }
		public int ErrorCode { get; }
		public string ErrorMessage { get; }

		public TwitchVodPlaylistResult(TwitchVodPlaylist playlist, int errorCode, string errorMessage)
		{
			Playlist = playlist;
			ErrorCode = errorCode;
			ErrorMessage = errorMessage;
		}
	}
}
