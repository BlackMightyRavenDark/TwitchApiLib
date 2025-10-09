using System;
using System.Collections.Generic;

namespace TwitchApiLib.ConsoleTest
{
	internal class Program
	{
		static void Main(string[] args)
		{
			Console.Write("Enter channel/user name: ");
			string userName = Console.ReadLine();
			if (!string.IsNullOrEmpty(userName))
			{
				if (userName.Contains(" "))
				{
					Console.WriteLine("Error! User name must not contain spaces!");
					Console.ReadLine();
					return;
				}

				MultiThreadedDownloaderLib.Utils.ConnectionLimit = 100;
				const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:129.0) Gecko/20100101 Firefox/129.0";
				TwitchApi.SetUserAgent(userAgent);

				Console.WriteLine("Retrieving user info...");
				TwitchUserResult twitchUserResult = TwitchUser.Get(userName.ToLower());
				if (twitchUserResult.ErrorCode == 200)
				{
					Console.WriteLine($"User ID: {twitchUserResult.User.Id}");
					Console.WriteLine($"User login: {twitchUserResult.User.Login}");
					Console.WriteLine($"Display name: {twitchUserResult.User.DisplayName}");
					Console.WriteLine($"Channel URL: {twitchUserResult.User.ChannelUrl}");
					Console.WriteLine($"User type: {twitchUserResult.User.UserType}");
					Console.WriteLine($"Broadcaster type: {twitchUserResult.User.BroadcasterType}");
					Console.WriteLine($"Live stream access mode: {twitchUserResult.User.StreamPlaybackAccessMode}");
					Console.WriteLine($"User description: {twitchUserResult.User.Description}");
					Console.WriteLine($"Profile image url template: {twitchUserResult.User.ProfileImageUrl}");
					Console.WriteLine($"Offline image url template: {twitchUserResult.User.OfflineImageUrl}");
					Console.WriteLine($"View count: {twitchUserResult.User.ViewCount}");
					Console.WriteLine($"Creation date: {twitchUserResult.User.CreationDate.FormatDateTime()}");
					Console.WriteLine();

					Console.WriteLine("Retrieving channel videos...");

					List<TwitchVodResult> vods = twitchUserResult.User.GetVideosMultiThreaded(10U);

					if (vods.Count > 0)
					{
						Console.WriteLine($"Videos found: {vods.Count}{Environment.NewLine}");

						vods.Sort((x, y) =>
						{
							if (x.ErrorCode != 200 || y.ErrorCode != 200) { return 0; }

							return x.Vod.CreationDate > y.Vod.CreationDate ? -1 : 1;
						});

						foreach (TwitchVodResult vodResult in vods)
						{
							if (vodResult.ErrorCode != 200)
							{
								Console.Error.WriteLine(vodResult.ErrorMessage);
								continue;
							}

							Console.WriteLine($"VOD ID: {vodResult.Vod.Id}");
							Console.WriteLine($"VOD type: {vodResult.Vod.VodType}");
							Console.WriteLine($"Title: {vodResult.Vod.Title}");
							Console.WriteLine($"Description: {vodResult.Vod.Description}");
							Console.WriteLine($"Duration: {vodResult.Vod.Duration}");
							if (vodResult.Vod.Game != null)
							{
								string gameName = vodResult.Vod.Game.IsKnown ?
									vodResult.Vod.Game.DisplayName : "<not detected>";
								Console.WriteLine($"Game: {gameName}");
								if (vodResult.Vod.Game.IsKnown)
								{
									Console.WriteLine($"Game ID: {vodResult.Vod.Game.Id}");
								}

							}
							else
							{
								Console.WriteLine("Game: null");
							}
							Console.WriteLine($"Creation date: {vodResult.Vod.CreationDate.FormatDateTime()}");
							Console.WriteLine($"Published date: {vodResult.Vod.PublishedDate.FormatDateTime()}");
							if (vodResult.Vod.VodType != TwitchApi.TwitchVodType.Highlight &&
								vodResult.Vod.DeletionDate < DateTime.MaxValue)
							{
								Console.WriteLine($"Deletion date: {vodResult.Vod.DeletionDate.FormatDateTime()}");
							}
							Console.WriteLine($"Url: {vodResult.Vod.Url}");
							Console.WriteLine($"Access mode: {vodResult.Vod.PlaybackAccessMode}");
							Console.WriteLine($"Thumbnail template URL: {vodResult.Vod.ThumbnailUrlTemplate}");
							//TODO: Find a way to determine actual video resolution.
							Console.WriteLine($"Thumbnail URL: {vodResult.Vod.FormatThumbnailTemplateUrl(1920, 1080)}");
							Console.WriteLine($"Viewable: {vodResult.Vod.Viewable}");
							Console.WriteLine($"View count: {vodResult.Vod.ViewCount}");
							Console.WriteLine($"Language: {vodResult.Vod.Language}");
							Console.WriteLine($"Is live: {vodResult.Vod.IsLive}");
							if (vodResult.Vod.StreamId > 0UL)
							{
								Console.WriteLine($"Stream ID: {vodResult.Vod.StreamId}");
							}
							/*Console.WriteLine($"User ID: {vodResult.Vod.User?.Id}");
							Console.WriteLine($"User login: {vodResult.Vod.User?.Login}");
							Console.WriteLine($"User name: {vodResult.Vod.User?.DisplayName}");*/
							Console.WriteLine($"Playlist URL: {vodResult.Vod.PlaylistUrl}");
							Console.WriteLine();
						}
					}
					else
					{
						Console.WriteLine("No videos found!");
					}
				}
				else
				{
					Console.WriteLine($"User {userName} is not found! Error code: {twitchUserResult.ErrorCode}");
				}
			}
			else
			{
				Console.WriteLine("User name is empty. Press ENTER to EXIT...");
			}

			Console.ReadLine();
		}
	}
}
