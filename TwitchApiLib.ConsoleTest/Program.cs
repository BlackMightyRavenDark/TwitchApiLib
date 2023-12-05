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

                MultiThreadedDownloaderLib.MultiThreadedDownloader.SetMaximumConnectionsLimit(100);

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

                    List<TwitchVodResult> vods = twitchUserResult.User.GetVideosMultiThreaded(100U);

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
                            Console.WriteLine($"Title: {vodResult.Vod.Title}");
                            Console.WriteLine($"Description: {vodResult.Vod.Description}");
                            Console.WriteLine($"Duration: {vodResult.Vod.Duration}");
                            if (vodResult.Vod.Game != null)
                            {
                                string game = vodResult.Vod.Game.IsKnown ?
                                    $"{vodResult.Vod.Game.DisplayName} | {vodResult.Vod.Game.Id}" : "<not detected>";
                                Console.WriteLine($"Game: {game}");
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
                            Console.WriteLine($"Thumbnail template URL: {vodResult.Vod.ThumbnailUrl}");
                            Console.WriteLine($"Viewable: {vodResult.Vod.Viewable}");
                            Console.WriteLine($"View count: {vodResult.Vod.ViewCount}");
                            Console.WriteLine($"Language: {vodResult.Vod.Language}");
                            Console.WriteLine($"VOD type: {vodResult.Vod.VodType}");
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
