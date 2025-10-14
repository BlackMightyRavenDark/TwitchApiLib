
namespace TwitchApiLib
{
	public class TwitchVodPlaylistResult
	{
		public TwitchVodPlaylist Playlist { get; }
		public int ErrorCode { get; }

		public TwitchVodPlaylistResult(TwitchVodPlaylist playlist, int errorCode)
		{
			Playlist = playlist;
			ErrorCode = errorCode;
		}
	}
}
