using System;

namespace TwitchApiLib
{
	public class TwitchChannelLiveInfo
	{
		public ulong Id { get; }
		public ulong UserId { get; }
		public string UserLogin { get; }
		public string UserName { get; }
		public ulong GameId { get; }
		public string GameName { get; }
		public string StreamType { get; }
		public string StreamTitle { get; }
		public uint ViewerCount { get; }
		public DateTime StartedAt { get; }
		public string LanguageCode { get; }
		public string ThumbnailUrlTemplate { get; }
		public string[] Tags { get; }
		public bool IsMature { get; }

		public TwitchChannelLiveInfo(
			ulong id, ulong userId, string userLogin, string userName,
			ulong gameId, string gameName, string streamType, string streamTitle,
			uint viewerCount, DateTime startedAt, string languageCode,
			string thumbnailUrlTemplate, string[] tags, bool isMature)
		{
			Id = id;
			UserId = userId;
			UserLogin = userLogin;
			UserName = userName;
			GameId = gameId;
			GameName = gameName;
			StreamType = streamType;
			StreamTitle = streamTitle;
			ViewerCount = viewerCount;
			StartedAt = startedAt;
			LanguageCode = languageCode;
			ThumbnailUrlTemplate = thumbnailUrlTemplate;
			Tags = tags;
			IsMature = isMature;
		}

		public static TwitchChannelLiveInfoResult Get(ulong channelId)
		{
			return Utils.GetChannelLiveInfo_Helix(channelId);
		}

		public static TwitchChannelLiveInfoResult Get(string userLogin)
		{
			return Utils.GetChannelLiveInfo_Helix(userLogin);
		}

		public string FormatThumbnailTemplateUrl(ushort imageWidth, ushort imageHeight)
		{
			return ThumbnailUrlTemplate?
				.Replace("{width}", imageWidth.ToString())
				.Replace("{height}", imageHeight.ToString());
		}
	}
}
