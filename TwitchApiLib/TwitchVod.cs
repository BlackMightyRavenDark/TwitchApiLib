using System;
using System.IO;
using MultiThreadedDownloaderLib;
using static TwitchApiLib.TwitchApi;

namespace TwitchApiLib
{
	public class TwitchVod : IDisposable
	{
		public ulong Id { get; }
		public string Title { get; }
		public string Description { get; }
		public TimeSpan Duration { get; }
		public TwitchGame Game { get; }
		public DateTime CreationDate { get; }
		public DateTime PublishedDate { get; }
		public DateTime DeletionDate { get; }
		public string Url { get; }
		public string ThumbnailUrlTemplate { get; }
		public string Viewable { get; }
		public ulong ViewCount { get; }
		public string Language { get; }
		public TwitchVodType VodType { get; }
		public TwitchPlaybackAccessMode PlaybackAccessMode { get; }
		public bool IsLive { get; }
		public ulong StreamId { get; }
		public TwitchUser User { get; }
		public Stream PreviewImageData { get; private set; }
		public TwitchVodPlaylist Playlist => PlaylistManifest != null && PlaylistManifest.Count > 0 ? PlaylistManifest[0].Playlist : null;
		public TwitchVodPlaylistManifest PlaylistManifest { get; private set; }
		public string RawData { get; }
		public TwitchVideoMetadata RawMetadata { get; }
		public bool IsHighlight => VodType == TwitchVodType.Highlight;
		public bool IsUpload => VodType == TwitchVodType.Upload;
		public bool IsSubscribersOnly => PlaybackAccessMode == TwitchPlaybackAccessMode.SubscribersOnly;

		public TwitchVod(ulong id, string title, string description, TimeSpan duration,
			TwitchGame game, DateTime creationDate, DateTime publishedDate, DateTime deletionDate,
			string url, string thumbnailUrlTemplate, string viewable, ulong viewCount,
			string language, TwitchVodType vodType, TwitchPlaybackAccessMode playbackAccessMode,
			ulong streamId, TwitchUser user, string playlistUrl,
			string rawData, TwitchVideoMetadata rawMetadata)
		{
			Id = id;
			Title = title;
			Description = description;
			Duration = duration;
			Game = game;
			CreationDate = creationDate;
			PublishedDate = publishedDate;
			DeletionDate = deletionDate;
			Url = url;
			ThumbnailUrlTemplate = thumbnailUrlTemplate;
			Viewable = viewable;
			ViewCount = viewCount;
			Language = language;
			VodType = vodType;
			PlaybackAccessMode = playbackAccessMode;
			IsLive = GetIsLive();
			StreamId = streamId;
			User = user;
			RawData = rawData;
			RawMetadata = rawMetadata;
		}

		public void Dispose()
		{
			DisposePreviewImageData();
		}

		public static TwitchVodResult Get(ulong vodId)
		{
			return Utils.GetTwitchVodInfo(vodId);
		}

		private bool GetIsLive()
		{
			return !string.IsNullOrEmpty(ThumbnailUrlTemplate) &&
				ThumbnailUrlTemplate.Contains("_404/404_processing_");
		}

		public int GeneratePlaylistUrl(out string playlistUrl, string formatId = "chunked")
		{
			return Utils.GenerateVodPlaylistUrl(this, formatId, out playlistUrl);
		}

		public int GetPlaylistManifestUrl(out string manifestUrl)
		{
			return Utils.GetVodPlaylistManifestUrl(this, out manifestUrl);
		}

		public TwitchVodPlaylistManifestResult GetPlaylistManifest()
		{
			return Utils.GetVodPlaylistManifest(this);
		}

		public TwitchVodPlaylistResult GetPlaylist(string formatId)
		{
			int errorCode = GeneratePlaylistUrl(out string playlistUrl, formatId);
			if (errorCode == 200)
			{
				errorCode = Utils.DownloadString(playlistUrl, out string playlistRaw);
				if (errorCode == 200)
				{
					TwitchVodPlaylist playlist = new TwitchVodPlaylist(playlistRaw, playlistUrl, null);
					return new TwitchVodPlaylistResult(playlist, 200);
				}
			}

			return new TwitchVodPlaylistResult(null, errorCode);
		}

		public int GetPlaybackAccessToken(out ITwitchPlaybackAccessToken token, out string errorMessage)
		{
			return Utils.GetVodPlaybackAccessToken(Id, out token, out errorMessage);
		}

		public int GetPlaybackAccessToken(out ITwitchPlaybackAccessToken token)
		{
			return GetPlaybackAccessToken(out token, out _);
		}

		public int UpdatePlaylistManifest(bool autosort = true)
		{
			TwitchVodPlaylistManifestResult playlistManifestResult = GetPlaylistManifest();
			if (playlistManifestResult.ErrorCode == 200)
			{
				if (autosort && playlistManifestResult.PlaylistManifest.Parse() > 1)
				{
					playlistManifestResult.PlaylistManifest.SortByBandwidth();
				}
				PlaylistManifest = playlistManifestResult.PlaylistManifest;
			}

			return playlistManifestResult.ErrorCode;
		}

		public void ExtractSpecialData(out string specialId, out string serverId)
		{
			Utils.ExtractVodSpecialDataFromThumbnailUrl(ThumbnailUrlTemplate, out specialId, out serverId);
		}

		public string FormatThumbnailTemplateUrl(ushort imageWidth, ushort imageHeight)
		{
			return ThumbnailUrlTemplate?
				.Replace("%{width}", imageWidth.ToString())
				.Replace("%{height}", imageHeight.ToString());
		}

		public TwitchVodMutedSegments GetMutedSegments(bool showChunkCount = false)
		{
			return Playlist?.GetMutedSegments(showChunkCount);
		}

		public int RetrievePreviewImage(ushort width, ushort height)
		{
			if (PreviewImageData != null) { return 200; }

			if (string.IsNullOrEmpty(ThumbnailUrlTemplate) ||
				string.IsNullOrWhiteSpace(ThumbnailUrlTemplate))
			{
				return 400;
			}

			string url = FormatThumbnailTemplateUrl(width, height);

			if (!string.IsNullOrEmpty(url) && !string.IsNullOrWhiteSpace(url))
			{
				FileDownloader d = new FileDownloader() { Url = url };
				PreviewImageData = new MemoryStream();
				int errorCode = d.Download(PreviewImageData);

				if (errorCode != 200) { DisposePreviewImageData(); }

				return errorCode;
			}

			return 400;
		}

		public void DisposePreviewImageData()
		{
			if (PreviewImageData != null)
			{
				PreviewImageData.Close();
				PreviewImageData = null;
			}
		}
	}
}
