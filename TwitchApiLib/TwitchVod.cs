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
		public string ThumbnailUrl { get; }
		public string Viewable { get; }
		public ulong ViewCount { get; }
		public string Language { get; }
		public TwitchVodType VodType { get; }
		public TwitchPlaybackAccessMode PlaybackAccessMode { get; }
		public bool IsLive { get; }
		public ulong StreamId { get; }
		public TwitchUser User { get; }
		public Stream PreviewImageData { get; private set; }
		public string PlaylistUrl { get; }
		public string RawData { get; }
		public TwitchVideoMetadata RawMetadata { get; }
		public bool IsHighlight => VodType == TwitchVodType.Highlight;
		public bool IsUpload => VodType == TwitchVodType.Upload;
		public bool IsSubscribersOnly => PlaybackAccessMode == TwitchPlaybackAccessMode.SubscribersOnly;
		public bool IsPlaylistUrlExists { get; }

		public TwitchVod(ulong id, string title, string description, TimeSpan duration,
			TwitchGame game, DateTime creationDate, DateTime publishedDate, DateTime deletionDate,
			string url, string thumbnailUrl, string viewable, ulong viewCount,
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
			ThumbnailUrl = thumbnailUrl;
			Viewable = viewable;
			ViewCount = viewCount;
			Language = language;
			VodType = vodType;
			PlaybackAccessMode = playbackAccessMode;
			IsLive = GetIsLive();
			StreamId = streamId;
			User = user;
			PlaylistUrl = playlistUrl;
			RawData = rawData;
			RawMetadata = rawMetadata;
			IsPlaylistUrlExists = !string.IsNullOrEmpty(PlaylistUrl) && !string.IsNullOrWhiteSpace(PlaylistUrl);
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
			return !string.IsNullOrEmpty(ThumbnailUrl) &&
				ThumbnailUrl.Contains("_404/404_processing_");
		}

		public int GetPlaylistUrl(out string playlistUrl, string formatId = "chunked")
		{
			return Utils.GetVodPlaylistUrl(this, formatId, out playlistUrl);
		}

		public int GetPlaylistManifestUrl(out string manifestUrl)
		{
			return Utils.GetVodPlaylistManifestUrl(this, out manifestUrl);
		}

		public int GetPlaylistManifest(out TwitchVodPlaylistManifest manifest)
		{
			return Utils.GetVodPlaylistManifest(this, out manifest);
		}

		public TwitchVodPlaylistManifest GetPlaylistManifest()
		{
			return Utils.GetVodPlaylistManifest(this, out TwitchVodPlaylistManifest manifest) == 200 ? manifest : null;
		}

		public int GetPlaylist(out TwitchVodPlaylist playlist, string formatId = "chunked")
		{
			int errorCode = GetPlaylistUrl(out string playlistUrl, formatId);
			if (errorCode == 200)
			{
				FileDownloader d = new FileDownloader() { Url = playlistUrl };
				errorCode = d.DownloadString(out string playlistRaw);
				d.Dispose();
				if (errorCode == 200)
				{
					playlist = new TwitchVodPlaylist(playlistRaw, playlistUrl);
					return 200;
				}
			}

			playlist = null;
			return errorCode;
		}

		public TwitchVodPlaylist GetPlaylist(string formatId = "chunked")
		{
			int errorCode = GetPlaylist(out TwitchVodPlaylist playlist, formatId);
			return errorCode == 200 ? playlist : null;
		}

		public void GetSpecialData(out string specialId, out string serverId)
		{
			Utils.ExtractVodSpecialDataFromThumbnailUrl(ThumbnailUrl, out specialId, out serverId);
		}

		public string FormatThumbnailUrl(ushort imageWidth, ushort imageHeight)
		{
			return ThumbnailUrl?
				.Replace("%{width}", imageWidth.ToString())
				.Replace("%{height}", imageHeight.ToString());
		}

		public TwitchVodMutedSegments GetMutedSegments()
		{
			TwitchVodPlaylist playlist = GetPlaylist();
			if (playlist != null)
			{
				if (playlist.Parse() > 0)
				{
					TwitchVodMutedSegments mutedSegments =
						TwitchVodMutedSegments.ParseMutedSegments(playlist.ChunkList);
					mutedSegments.BuildSegmentList();
					mutedSegments.CalculateTotalDuration();
					return mutedSegments;
				}
			}

			return null;
		}

		public int RetrievePreviewImage(ushort width, ushort height)
		{
			if (PreviewImageData != null) { return 200; }

			if (string.IsNullOrEmpty(ThumbnailUrl) || string.IsNullOrWhiteSpace(ThumbnailUrl))
			{
				return 400;
			}

			string url = FormatThumbnailUrl(width, height);

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
