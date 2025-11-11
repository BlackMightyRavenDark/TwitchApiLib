using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using static TwitchApiLib.TwitchApi;
using static TwitchApiLib.Utils;

namespace TwitchApiLib
{
	public class TwitchUser
	{
		public ulong Id { get; }
		public string Login { get; }
		public string DisplayName { get; }
		public string UserType { get; }
		public TwitchBroadcasterType BroadcasterType { get; }
		public TwitchPlaybackAccessMode StreamPlaybackAccessMode { get; }
		public TwitchChannelLiveInfo LiveStreamInfo { get; private set; }
		public string Description { get; }
		public string ProfileImageUrl { get; }
		public string OfflineImageUrl { get; }
		public ulong ViewCount { get; }
		public string ChannelUrl { get; }
		public DateTime CreationDate { get; }
		public string RawData { get; }

		public TwitchUser(ulong id, string login, string displayName,
			string userType, TwitchBroadcasterType broadcasterType,
			TwitchPlaybackAccessMode streamPlaybackAccessMode, string description,
			string profileImageUrl, string offlineImageUrl,
			ulong viewCount, DateTime creationDate, string rawData)
		{
			Id = id;
			Login = login;
			DisplayName = displayName;
			UserType = userType;
			BroadcasterType = broadcasterType;
			StreamPlaybackAccessMode = streamPlaybackAccessMode;
			Description = description;
			ProfileImageUrl = profileImageUrl;
			OfflineImageUrl = offlineImageUrl;
			ViewCount = viewCount;
			ChannelUrl = $"{TWITCH_URL}/{login}";
			CreationDate = creationDate;
			RawData = rawData;
		}

		public static TwitchUserResult Get(string userLogin, bool getFromCache = true)
		{
			if (getFromCache && _twitchUserLogins.ContainsKey(userLogin) &&
				_twitchUserLogins.TryGetValue(userLogin, out TwitchUser cachedUser))
			{
				return new TwitchUserResult(cachedUser, 200);
			}

			int errorCode = FindRawUserInfo(userLogin, out JObject response);
			if (errorCode == 200)
			{
				TwitchUserResult userResult = ParseUserSearchResult(response);
				if (userResult.ErrorCode == 200)
				{
					_twitchUserLogins[userLogin] = userResult.User;
					_twitchUserIds[userResult.User.Id] = userResult.User;

					return userResult;
				}
			}

			return new TwitchUserResult(null, errorCode);
		}

		public static TwitchUserResult Get(ulong userId, bool getFromCache = true)
		{
			if (getFromCache && _twitchUserIds.ContainsKey(userId) &&
				_twitchUserIds.TryGetValue(userId, out TwitchUser cachedUser))
			{
				return new TwitchUserResult(cachedUser, 200);
			}

			int errorCode = FindRawUserInfo(userId, out JObject response);
			if (errorCode == 200)
			{
				TwitchUserResult userResult = ParseUserSearchResult(response);
				if (userResult.ErrorCode == 200)
				{
					_twitchUserLogins[userResult.User.Login] = userResult.User;
					_twitchUserIds[userId] = userResult.User;

					return userResult;
				}
			}

			return new TwitchUserResult(null, errorCode);
		}

		public JArray GetVideosRaw(uint maxVideos, uint videosPerPage = 10U)
		{
			if (videosPerPage == 0U) { return null; }

			JArray jaResultVods = new JArray();
			string pageToken = null;

			while (true)
			{
				TwitchVodPageResult vodPageResult = TwitchVodPage.Get(Id, videosPerPage, pageToken);
				if (vodPageResult.ErrorCode != 200 || vodPageResult.VodPage == null) { break; }

				JObject json = TryParseJson(vodPageResult.VodPage.RawData);
				if (json == null) { break; }

				JArray jaData = json.Value<JArray>("data");
				if (jaData == null || jaData.Count == 0) { break; }
				foreach (JObject jRawVod in jaData.Cast<JObject>())
				{
					jaResultVods.Add(jRawVod);
					if (maxVideos > 0U && jaResultVods.Count >= maxVideos) { return jaResultVods; }
				}

				pageToken = vodPageResult.VodPage.GetNextPageToken();
				if (string.IsNullOrEmpty(pageToken) || string.IsNullOrWhiteSpace(pageToken)) { break; }
			}

			return jaResultVods;
		}

		public IEnumerable<TwitchVodPage> GetVideoPages(uint maxPages, uint videosPerPage = 10U)
		{
			string pageToken = null;
			uint pageCount = 0;

			while (true)
			{
				TwitchVodPageResult vodPageResult = TwitchVodPage.Get(Id, videosPerPage, pageToken);
				if (vodPageResult.ErrorCode != 200 || vodPageResult.VodPage == null) { break; }

				yield return vodPageResult.VodPage;
				pageCount++;
				if (maxPages > 0U && pageCount >= maxPages) { break; }

				pageToken = vodPageResult.VodPage.GetNextPageToken();
				if (string.IsNullOrEmpty(pageToken)) { break; }
			}
		}

		public JArray GetVideosRaw()
		{
			return GetVideosRaw(0U);
		}

		public IEnumerable<TwitchVodResult> GetVideos(uint maxVideos, uint videosPerPage = 10U)
		{
			JArray jaRawVods = GetVideosRaw(maxVideos, videosPerPage);
			if (jaRawVods.Count == 0) { yield break; }

			uint vodCount = 0;
			foreach (JObject jVod in jaRawVods.Cast<JObject>())
			{
				TwitchVodResult vodResult = TwitchVod.Parse(jVod);
				yield return vodResult;
				vodCount++;

				if (maxVideos > 0U && vodCount >= maxVideos) { break; }
			}
		}

		public IEnumerable<TwitchVodResult> GetVideos()
		{
			return GetVideos(0U);
		}

		public IEnumerable<TwitchVodResult> GetVideosMultiThreaded(uint maxVideos, byte simultaneousThreads,
			int millisecondsTimeout, CancellationToken cancellationToken = default)
		{
			JArray jaRawVods = GetVideosRaw(maxVideos);
			if (jaRawVods.Count == 0) { yield break; }

			if (simultaneousThreads <= 0) { simultaneousThreads = 2; }

			ConcurrentBag<TwitchVodResult> bag = new ConcurrentBag<TwitchVodResult>();

			for (int i = 0; i < jaRawVods.Count; i += simultaneousThreads)
			{
				var group = GetGroup(jaRawVods, i, simultaneousThreads);
				if (group.Count() == 0) { break; }

				var tasks = group.Select(jVod => Task.Run(() =>
				{
					TwitchVodResult vodResult = TwitchVod.Parse(jVod);
					bag.Add(vodResult);
				}));

				if (millisecondsTimeout > 0)
				{
					Task.WhenAll(tasks).Wait(millisecondsTimeout, cancellationToken);
				}
				else
				{
					Task.WhenAll(tasks).Wait(cancellationToken);
				}
			}

			if (bag.Count > 0)
			{
				foreach (TwitchVodResult bagItem in bag)
				{
					yield return bagItem;
				}
			}
		}

		public IEnumerable<TwitchVodResult> GetVideosMultiThreaded(uint maxVideos, byte simultaneousThreads,
			CancellationToken cancellationToken)
		{
			return GetVideosMultiThreaded(maxVideos, simultaneousThreads, 0, cancellationToken);
		}

		public IEnumerable<TwitchVodResult> GetVideosMultiThreaded(uint maxVideos, CancellationToken cancellationToken)
		{
			return GetVideosMultiThreaded(maxVideos, 5, cancellationToken);
		}

		public IEnumerable<TwitchVodResult> GetVideosMultiThreaded(uint maxVideos, byte simultaneousThreads = 5)
		{
			return GetVideosMultiThreaded(maxVideos, simultaneousThreads, default);
		}

		public bool UpdateLiveStreamInfo(out TwitchChannelLiveInfoResult channelLiveInfoResult)
		{
			channelLiveInfoResult = TwitchChannelLiveInfo.Get(Id);
			LiveStreamInfo = channelLiveInfoResult.LiveInfo;
			return LiveStreamInfo != null;
		}

		public bool UpdateLiveStreamInfo()
		{
			return UpdateLiveStreamInfo(out _);
		}

		public static TwitchUserResult Parse(JObject json)
		{
			ulong userId = json.Value<ulong>("id");
			string userLogin = json.Value<string>("login");
			string displayName = json.Value<string>("display_name");
			string userType = json.Value<string>("type");
			string broadcasterTypeString = json.Value<string>("broadcaster_type");
			string description = json.Value<string>("description");
			string profileImageUrl = json.Value<string>("profile_image_url");
			string offlineImageUrl = json.Value<string>("offline_image_url");
			ulong viewCount = json.Value<ulong>("view_count");
			string createdAt = json.Value<string>("created_at");
			DateTime creationDate = ParseDateTime(createdAt);

			TwitchBroadcasterType broadcasterType = GetBroadcasterType(broadcasterTypeString);
			TwitchPlaybackAccessMode playbackAccessMode = TwitchApiGql.GetChannelPlaybackAccessMode(userLogin, out _);

			TwitchUser user = new TwitchUser(userId, userLogin, displayName, userType, broadcasterType, playbackAccessMode,
				description, profileImageUrl, offlineImageUrl, viewCount, creationDate, json.ToString());
			return new TwitchUserResult(user, 200);
		}

		public static TwitchUserResult Parse(string rawData)
		{
			JObject json = TryParseJson(rawData);
			return json != null ? Parse(json) : new TwitchUserResult(null, 400);
		}

		public static TwitchUserResult ParseUserSearchResult(JObject searchResult)
		{
			JArray jaData = searchResult?.Value<JArray>("data");
			if (jaData == null || jaData.Count == 0)
			{
				return new TwitchUserResult(null, 404);
			}

			return Parse(jaData[0] as JObject);
		}

		private static IEnumerable<JObject> GetGroup(JArray sourceArray, int startId, int groupSize)
		{
			int lastId = startId + groupSize - 1;
			for (int i = startId; i <= lastId && i < sourceArray.Count; ++i)
			{
				yield return sourceArray[i] as JObject;
			}
		}
	}
}
