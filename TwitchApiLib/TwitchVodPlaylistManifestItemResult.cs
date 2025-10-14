
namespace TwitchApiLib
{
	public class TwitchVodPlaylistManifestItemResult
	{
		public TwitchVodPlaylistManifestItem PlaylistManifestItem { get; }
		public int ErrorCode { get; }

		public TwitchVodPlaylistManifestItemResult(
			TwitchVodPlaylistManifestItem playlistManifestItem, int errorCode)
		{
			PlaylistManifestItem = playlistManifestItem;
			ErrorCode = errorCode;
		}
	}
}
