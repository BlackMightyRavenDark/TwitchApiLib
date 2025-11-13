using System;
using System.IO;
using Newtonsoft.Json.Linq;
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
		public Stream ThumbnailImageData { get; private set; }
		public TwitchVodPlaylist Playlist => PlaylistManifest != null && PlaylistManifest.Count > 0 ? PlaylistManifest[0].Playlist : null;
		public TwitchVodPlaylistManifest PlaylistManifest { get; private set; }
		public string RawData { get; }
		public bool IsHighlight => VodType == TwitchVodType.Highlight;
		public bool IsUpload => VodType == TwitchVodType.Upload;
		public bool IsSubscribersOnly => PlaybackAccessMode == TwitchPlaybackAccessMode.SubscribersOnly;
		public bool WillBeDeleted => VodType != TwitchVodType.Highlight && DeletionDate > DateTime.MinValue && DeletionDate < DateTime.MaxValue;

		public TwitchVod(ulong id, string title, string description, TimeSpan duration,
			TwitchGame game, DateTime creationDate, DateTime publishedDate, DateTime deletionDate,
			string url, string thumbnailUrlTemplate, string viewable, ulong viewCount,
			string language, TwitchVodType vodType, TwitchPlaybackAccessMode playbackAccessMode,
			ulong streamId, TwitchUser user, string rawData)
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
		}

		public void Dispose()
		{
			DisposeThumbnailImageData();
		}

		public static TwitchVodResult Get(ulong vodId)
		{
			string url = Utils.GenerateVodInfoRequestUrl(vodId);
			int errorCode = Utils.HttpGet_Helix(url, out string response, out _);
			if (errorCode == 200)
			{
				JObject json = Utils.TryParseJson(response, out string parsingResult);
				if (json == null)
				{
					return new TwitchVodResult(null, 400, parsingResult, response);
				}

				JArray jaData = json.Value<JArray>("data");
				if (jaData == null)
				{
					return new TwitchVodResult(null, 204, "'data' not found", response);
				}

				if (jaData.Count == 0)
				{
					return new TwitchVodResult(null, 204, "The 'data' is empty", response);
				}

				return Parse(jaData[0] as JObject);
			}

			return new TwitchVodResult(null, errorCode, null, response);
		}

		public static TwitchVodResult Parse(JObject vodInfo)
		{
			try
			{
				ulong vodId = vodInfo.Value<ulong>("id");
				JToken jt = vodInfo.Value<JToken>("stream_id");
				ulong streamId = jt != null && jt.Type != JTokenType.Null ? jt.Value<ulong>() : 0U;
				string userLogin = vodInfo.Value<string>("user_login");
				string title = vodInfo.Value<string>("title");
				string description = vodInfo.Value<string>("description");
				string createdAt = vodInfo.Value<string>("created_at");
				DateTime creationDate = Utils.ParseDateTime(createdAt);
				string publishedAt = vodInfo.Value<string>("published_at");
				DateTime publishedDate = Utils.ParseDateTime(publishedAt);
				string durationString = vodInfo.Value<string>("duration");
				string url = vodInfo.Value<string>("url");
				string thumbnailTemplateUrl = vodInfo.Value<string>("thumbnail_url");
				string viewable = vodInfo.Value<string>("viewable");
				ulong viewCount = vodInfo.Value<ulong>("view_count");
				string language = vodInfo.Value<string>("language");
				string vodTypeString = vodInfo.Value<string>("type");

				TwitchVodType vodType = Utils.GetVodType(vodTypeString);

				TimeSpan duration = Utils.ParseVodDuration(durationString);

				TwitchUserResult twitchUserResult = TwitchUser.Get(userLogin);
				DateTime deletionDate = DateTime.MaxValue;
				if (vodType == TwitchVodType.Archive && twitchUserResult.ErrorCode == 200 && twitchUserResult.User != null)
				{
					bool isPartner = twitchUserResult.User.BroadcasterType == TwitchBroadcasterType.Partner;
					deletionDate = creationDate.AddDays(isPartner ? 60.0 : 14.0);
				}

				TwitchGameResult gameResult = TwitchApiGql.GetVodGameInfo(vodId.ToString());
				TwitchPlaybackAccessMode playbackAccessMode = TwitchApiGql.GetVodPlaybackAccessMode(vodId.ToString(), out _);

				TwitchVod vod = new TwitchVod(vodId, title, description, duration, gameResult.Game, creationDate,
					publishedDate, deletionDate, url, thumbnailTemplateUrl, viewable, viewCount,
					language, vodType, playbackAccessMode, streamId, twitchUserResult.User,
					vodInfo.ToString());
				if (vod.UpdatePlaylistManifest() == 200 && vod.PlaylistManifest.Count > 0)
				{
					vod.PlaylistManifest[0].UpdatePlaylist();
				}

				return new TwitchVodResult(vod, 200, null, vodInfo.ToString());
			}
			catch (Exception ex)
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine(ex.Message);
#endif
				return new TwitchVodResult(null, ex.HResult, ex.Message, vodInfo?.ToString());
			}
		}

		public static TwitchVodResult Parse(string rawVodInfo)
		{
			JObject json = Utils.TryParseJson(rawVodInfo, out string errorMessage);
			return json != null ? Parse(json) : new TwitchVodResult(null, 400, $"Can't parse JSON! {errorMessage}", rawVodInfo);
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
			return TwitchVodPlaylistManifest.Get(this);
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

		public int ReceiveThumbnail(ushort width, ushort height)
		{
			if (ThumbnailImageData != null) { return 200; }

			if (string.IsNullOrEmpty(ThumbnailUrlTemplate) ||
				string.IsNullOrWhiteSpace(ThumbnailUrlTemplate))
			{
				return 400;
			}

			string url = FormatThumbnailTemplateUrl(width, height);

			if (!string.IsNullOrEmpty(url) && !string.IsNullOrWhiteSpace(url))
			{
				int timeout = GetConnectionTimeout();
				FileDownloader d = new FileDownloader() { Url = url, ConnectionTimeout = timeout };
				ThumbnailImageData = new MemoryStream();
				int errorCode = d.Download(ThumbnailImageData);

				if (errorCode != 200) { DisposeThumbnailImageData(); }

				return errorCode;
			}

			return 400;
		}

		public void DisposeThumbnailImageData()
		{
			if (ThumbnailImageData != null)
			{
				ThumbnailImageData.Close();
				ThumbnailImageData = null;
			}
		}
	}
}
