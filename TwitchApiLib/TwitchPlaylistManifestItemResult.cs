
namespace TwitchApiLib
{
	public class TwitchPlaylistManifestItemResult
	{
		public TwitchPlaylistManifestItem PlaylistManifestItem { get; }
		public int ErrorCode { get; }

		public TwitchPlaylistManifestItemResult(
			TwitchPlaylistManifestItem playlistManifestItem, int errorCode)
		{
			PlaylistManifestItem = playlistManifestItem;
			ErrorCode = errorCode;
		}
	}
}
