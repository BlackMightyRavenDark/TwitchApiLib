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
				errorCode = ParseRawUserInfo(response, out TwitchUser user);
				if (errorCode == 200)
				{
					_twitchUserLogins[userLogin] = user;
					_twitchUserIds[user.Id] = user;
				}

				return new TwitchUserResult(user, errorCode);
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
				errorCode = ParseRawUserInfo(response, out TwitchUser user);
				if (errorCode == 200)
				{
					_twitchUserLogins[user.Login] = user;
					_twitchUserIds[userId] = user;
				}

				return new TwitchUserResult(user, errorCode);
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

		public List<TwitchVodPage> GetVideoPages(uint maxPages, uint videosPerPage = 10U)
		{
			List<TwitchVodPage> resultList = new List<TwitchVodPage>();
			string pageToken = null;

			while (true)
			{
				TwitchVodPageResult vodPageResult = TwitchVodPage.Get(Id, videosPerPage, pageToken);
				if (vodPageResult.ErrorCode != 200 || vodPageResult.VodPage == null) { break; }

				resultList.Add(vodPageResult.VodPage);
				if (maxPages > 0U && resultList.Count >= maxPages) { break; }

				pageToken = vodPageResult.VodPage.GetNextPageToken();
				if (string.IsNullOrEmpty(pageToken)) { break; }
			}

			return resultList;
		}

		public JArray GetVideosRaw()
		{
			return GetVideosRaw(0U);
		}

		public List<TwitchVodResult> GetVideos(uint maxVideos, uint videosPerPage = 10U)
		{
			List<TwitchVodResult> resultList = new List<TwitchVodResult>();
			JArray jaRawVods = GetVideosRaw(maxVideos, videosPerPage);
			if (jaRawVods.Count == 0)
			{
				return resultList;
			}

			foreach (JObject jVod in jaRawVods.Cast<JObject>())
			{
				TwitchVodResult vodResult = ParseVodInfo(jVod);
				resultList.Add(vodResult);

				if (maxVideos > 0U && resultList.Count >= maxVideos) { break; }
			}

			return resultList;
		}

		public List<TwitchVodResult> GetVideos()
		{
			return GetVideos(0U);
		}

		public List<TwitchVodResult> GetVideosMultiThreaded(uint maxVideos, byte simultaneousThreads,
			int millisecondsTimeout, CancellationToken cancellationToken = default)
		{
			List<TwitchVodResult> resultList = new List<TwitchVodResult>();

			JArray jaRawVods = GetVideosRaw(maxVideos);
			if (jaRawVods.Count > 0)
			{
				if (simultaneousThreads <= 0) { simultaneousThreads = 2; }

				ConcurrentBag<TwitchVodResult> bag = new ConcurrentBag<TwitchVodResult>();

				for (int i = 0; i < jaRawVods.Count; i += simultaneousThreads)
				{
					var group = GetGroup(jaRawVods, i, simultaneousThreads);
					if (group.Count() == 0) { break; }

					var tasks = group.Select(jVod => Task.Run(() =>
					{
						TwitchVodResult vodResult = ParseVodInfo(jVod);
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
						resultList.Add(bagItem);
					}
				}
			}

			return resultList;
		}

		public List<TwitchVodResult> GetVideosMultiThreaded(uint maxVideos, byte simultaneousThreads,
			CancellationToken cancellationToken)
		{
			return GetVideosMultiThreaded(maxVideos, simultaneousThreads, 0, cancellationToken);
		}

		public List<TwitchVodResult> GetVideosMultiThreaded(uint maxVideos, CancellationToken cancellationToken)
		{
			return GetVideosMultiThreaded(maxVideos, 5, cancellationToken);
		}

		public List<TwitchVodResult> GetVideosMultiThreaded(uint maxVideos, byte simultaneousThreads = 5)
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

		private static bool IsUserExists(JObject searchResultsJson)
		{
			JArray jaData = searchResultsJson.Value<JArray>("data");
			return jaData != null && jaData.Count > 0;
		}

		public static int ParseRawUserInfo(JObject rawUserInfo, out TwitchUser result)
		{
			if (!IsUserExists(rawUserInfo))
			{
				result = null;
				return 404;
			}

			result = ParseTwitchUserInfo(rawUserInfo);
			return result != null ? 200 : 400;
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
