using System;

namespace TwitchApiLib
{
	public class TwitchVodPlaylistManifestItem
	{
		public int ResolutionWidth { get; }
		public int ResolutionHeight { get; }
		public int Bandwidth { get; }
		public int FrameRate { get; }
		public string Codecs { get; }
		public string FormatId { get; }
		public string PlaylistUrl { get; }
		public bool IsAudioOnly { get; }
		public bool IsBestQuality { get; }
		public TwitchVodPlaylist Playlist { get; set; }

		public TwitchVodPlaylistManifestItem(int resolutionWidth, int resolutionHeight, int bandwidth,
			int frameRate, string codecs, string formatId, string playlistUrl)
		{
			ResolutionWidth = resolutionWidth;
			ResolutionHeight = resolutionHeight;
			Bandwidth = bandwidth;
			FrameRate = frameRate;
			Codecs = codecs;
			FormatId = formatId;
			PlaylistUrl = playlistUrl;
			IsAudioOnly = !string.IsNullOrEmpty(FormatId) && FormatId.Contains("audio_only");
			IsBestQuality = string.Equals(formatId, "chunked", StringComparison.OrdinalIgnoreCase);
		}

		public static TwitchVodPlaylistManifestItemResult Get(TwitchVod vod, string formatId)
		{
			TwitchVodPlaylistManifestResult manifestResult = TwitchVodPlaylistManifest.Get(vod);
			if (manifestResult.ErrorCode == 200)
			{
				if (manifestResult.PlaylistManifest.Parse() > 0)
				{
					TwitchVodPlaylistManifestItem item = manifestResult.PlaylistManifest[formatId];
					int errorCode = item != null ? 200 : 404;
					return new TwitchVodPlaylistManifestItemResult(item, errorCode);
				}

				return new TwitchVodPlaylistManifestItemResult(null, 204);
			}

			return new TwitchVodPlaylistManifestItemResult(null, manifestResult.ErrorCode);
		}

		public TwitchVodPlaylistResult GetPlaylist()
		{
			if (!string.IsNullOrEmpty(PlaylistUrl) && !string.IsNullOrWhiteSpace(PlaylistUrl))
			{
				int errorCode = Utils.DownloadString(PlaylistUrl, out string response);
				TwitchVodPlaylistResult playlistResult = errorCode == 200 ?
					new TwitchVodPlaylistResult(new TwitchVodPlaylist(response, null, this), 200, null) :
					new TwitchVodPlaylistResult(null, errorCode, response);
				return playlistResult;
			}

			return new TwitchVodPlaylistResult(null, 400, null);
		}

		public int UpdatePlaylist(out string errorMessage, bool autoParsePlaylist = true)
		{
			int errorCode = Utils.DownloadString(PlaylistUrl, out string response);
			if (errorCode == 200)
			{
				errorMessage = null;
				Playlist = new TwitchVodPlaylist(response, PlaylistUrl, this);
				if (autoParsePlaylist) { Playlist.Parse(); }
			}
			else
			{
				errorMessage = response;
			}

			return errorCode;
		}

		public int UpdatePlaylist()
		{
			return UpdatePlaylist(out _);
		}

		public override string ToString()
		{
			string t = $"Format ID: {FormatId}{Environment.NewLine}" +
				$"The best quality: {(IsBestQuality ? "Yes" : "No")}{Environment.NewLine}";
			if (!IsAudioOnly)
			{
				t += $"Resolution: {ResolutionWidth}x{ResolutionHeight}{Environment.NewLine}";
				if (FrameRate > 0)
				{
					t += $"Frame rate: {FrameRate} fps{Environment.NewLine}";
				}
			}
			t += $"Bandwidth: {Bandwidth}{Environment.NewLine}" +
				$"Codecs: {Codecs}{Environment.NewLine}" +
				$"Playlist URL: {PlaylistUrl}{Environment.NewLine}";
			return t;
		}
	}
}
