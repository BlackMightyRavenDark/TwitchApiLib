using System;
using System.IO;
using MultiThreadedDownloaderLib;
using Newtonsoft.Json.Linq;
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
		public string PlaylistUrl { get; }
		public TwitchPlaylist Playlist { get; private set; }
		public string RawData { get; }
		public TwitchVideoMetadata RawMetadata { get; }
		public bool IsHighlight => VodType == TwitchVodType.Highlight;
		public bool IsUpload => VodType == TwitchVodType.Upload;
		public bool IsSubscribersOnly => PlaybackAccessMode == TwitchPlaybackAccessMode.SubscribersOnly;
		public bool IsPlaylistUrlExists { get; }

		public TwitchVod(ulong id, string title, string description, TimeSpan duration,
			TwitchGame game, DateTime creationDate, DateTime publishedDate, DateTime deletionDate,
			string url, string thumbnailUrlTemplate, string viewable, ulong viewCount,
			string language, TwitchVodType vodType, TwitchPlaybackAccessMode playbackAccessMode,
			ulong streamId, TwitchUser user, string playlistUrl, TwitchPlaylist playlist,
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
			PlaylistUrl = playlistUrl;
			Playlist = playlist;
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
			return !string.IsNullOrEmpty(ThumbnailUrlTemplate) &&
				ThumbnailUrlTemplate.Contains("_404/404_processing_");
		}

		public int GetPlaylistUrl(out string playlistUrl, string formatId = "chunked")
		{
			return Utils.GetVodPlaylistUrl(this, formatId, out playlistUrl);
		}

		public int GetPlaylistManifestUrl(out string manifestUrl)
		{
			return Utils.GetVodPlaylistManifestUrl(this, out manifestUrl);
		}

		public TwitchPlaylistManifestResult GetPlaylistManifest()
		{
			return Utils.GetVodPlaylistManifest(this);
		}

		public TwitchPlaylistResult GetPlaylist(string formatId = "chunked")
		{
			{
				TwitchPlaylistManifestItemResult manifestItemResult = Utils.GetVodPlaylistManifestItem(this, formatId);
				if (manifestItemResult.ErrorCode == 200)
				{
					int errorCode = Utils.DownloadString(manifestItemResult.PlaylistManifestItem.PlaylistUrl, out string playlistRaw);
					if (errorCode == 200)
					{
						TwitchPlaylist playlist = new TwitchPlaylist(playlistRaw, manifestItemResult.PlaylistManifestItem.PlaylistUrl);
						return new TwitchPlaylistResult(playlist, 200);
					}
				}
			}
			{
				int errorCode = GetPlaylistUrl(out string playlistUrl, formatId);
				if (errorCode == 200)
				{
					errorCode = Utils.DownloadString(playlistUrl, out string playlistRaw);
					if (errorCode == 200)
					{
						TwitchPlaylist playlist = new TwitchPlaylist(playlistRaw, playlistUrl);
						return new TwitchPlaylistResult(playlist, 200);
					}
				}

				return new TwitchPlaylistResult(null, errorCode);
			}
		}

		public int GetPlaybackAccessToken(out JObject token, out string errorMessage)
		{
			return Utils.GetVodPlaybackAccessToken(Id, out token, out errorMessage);
		}

		public int GetPlaybackAccessToken(out JObject token)
		{
			return GetPlaybackAccessToken(out token, out _);
		}

		public bool ClearPlaylist()
		{
			if (Playlist != null)
			{
				Playlist.ChunkList.Clear();
				return true;
			}

			return false;
		}

		public bool UpdatePlaylist(string formatId, bool clearCurrentPlaylist = false)
		{
			TwitchPlaylistResult playlistResult = GetPlaylist(formatId);
			if (clearCurrentPlaylist)
			{
				Playlist = playlistResult.ErrorCode == 200 ? playlistResult.Playlist : null;
				return Playlist != null;
			}

			if (playlistResult.ErrorCode == 200)
			{
				Playlist = playlistResult.Playlist;
				return true;
			}

			return false;
		}

		public bool UpdatePlaylist(bool clearCurrentPlaylist)
		{
			return UpdatePlaylist("chunked", clearCurrentPlaylist);
		}

		public bool UpdatePlaylist()
		{
			return UpdatePlaylist(false);
		}

		public void GetSpecialData(out string specialId, out string serverId)
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
			if (UpdatePlaylist()) { return Playlist.GetMutedSegments(showChunkCount); }

			return null;
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
