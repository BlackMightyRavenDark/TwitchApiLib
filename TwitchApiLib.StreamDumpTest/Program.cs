using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;
using MultiThreadedDownloaderLib;

namespace TwitchApiLib.StreamDumpTest
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

				/*
				 * Внимание! В данный момент представленные ниже ID и ключ могут оказаться устаревшими и не работать!
				 * Вы должны заменить их на собственные! Сгенерировать их можно на странице:
				 * https://dev.twitch.tv/console/
				 */
				TwitchApi.SetApplication(new TwitchApplication(
					"Test application", "No description",
					"gs7pui3law5lsi69yzi9qzyaqvlcsy", // Client ID
					"srr2yi260t15ir6w0wq5blir22i9pq"  // Secret key
					)
				);
				const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:129.0) Gecko/20100101 Firefox/129.0";
				TwitchApi.SetUserAgent(userAgent);

				Utils.TwitchHelixOauthToken.TokenUpdating += s => Console.WriteLine("Twitch Helix API token is updating...");
				Utils.TwitchHelixOauthToken.TokenUpdated += (s, errorCode) =>
				{
					if (errorCode == 200)
					{
						TwitchHelixOauthToken token = s as TwitchHelixOauthToken;
						Console.WriteLine($"Twitch Helix API token is successfully updated! New token: {token.AccessToken}, Expires at: {token.ExpirationDate.FormatDateTime()}");
					}
					else
					{
						Console.WriteLine($"There is an error while updating the Twitch Helix API token! Code {errorCode}.");
					}
				};

				Console.WriteLine("Receiving user info...");
				TwitchUserResult userResult = TwitchUser.Get(userName);
				if (userResult.ErrorCode != 200)
				{
					Console.WriteLine($"User not found! Error code: {userResult.ErrorCode}");
					Console.ReadLine();
					return;
				}

				Console.WriteLine("Searching for live stream...");
				if (userResult.User.UpdateLiveStreamInfo(out TwitchChannelLiveInfoResult liveInfoResult))
				{
					Console.WriteLine("Live stream found!");
					Console.WriteLine($"Title: {liveInfoResult.LiveInfo.StreamTitle}");
					Console.WriteLine($"Started at: {liveInfoResult.LiveInfo.StartedAt.FormatDateTime()}");

					TwitchVodPlaylistManifestResult manifestResult = liveInfoResult.LiveInfo.GetHlsPlaylistManifest(
						(s, url, downloader) =>
						{
							/*
							 * Здесь можно настроить прокси-сервер для получения плейлиста максимального качества.
							 * Можно скачать манифест вручную и вернуть его в качестве результата.
							 * Если вернуть значение 'null', манифест будет скачан автоматически (будут использованы параметры объекта 'downloader').
							 */
							return null;
						}, out string errorMessage);
					if (manifestResult.ErrorCode == 200)
					{
						if (manifestResult.PlaylistManifest.Parse() > 0)
						{
							manifestResult.PlaylistManifest.SortByBandwidth();
							TwitchVodPlaylistManifestItem item = manifestResult.PlaylistManifest[0];
							Console.Write("Format ID: ");
							if (item.IsBestQuality) { Console.ForegroundColor = ConsoleColor.Green; }
							Console.WriteLine(item.FormatId);
							Console.ForegroundColor = ConsoleColor.Gray;
							if (!item.IsAudioOnly())
							{
								Console.WriteLine($"Video: {item.ResolutionWidth}x{item.ResolutionHeight} | {item.FrameRate} fps");
							}
							Console.WriteLine($"Playlist URL: {item.PlaylistUrl}");

							List<TwitchVodChunk> chunkList = new List<TwitchVodChunk>();
							while (item.UpdatePlaylist() == 200)
							{
								Console.ForegroundColor = ConsoleColor.Gray;
								Console.Write("Playlist is updated! New first chunk ID: ");
								Console.ForegroundColor = ConsoleColor.Cyan;
								Console.WriteLine(item.Playlist.FirstChunkId);
								Console.ForegroundColor = ConsoleColor.Gray;

								var filtered = item.Playlist.ChunkList.Where(a => !chunkList.Any(b => b.FileName == a.FileName));
								int newChunkCount = filtered.Count();
								Console.WriteLine($"New chunks in playlist: {newChunkCount} / {item.Playlist.Count}");
								if (newChunkCount > 0)
								{
									Console.WriteLine("Downloading...");
									int startTime = Environment.TickCount;
									try
									{
										foreach (TwitchVodChunk chunk in filtered)
										{
											chunkList.Add(chunk);

											Console.Write("Chunk ID ");
											Console.ForegroundColor = ConsoleColor.Green;
											Console.Write(chunk.Id);
											Console.ForegroundColor = ConsoleColor.Gray;
											Console.WriteLine($": {chunk.FileUrl}");

											const string dumpDirectoryName = "dump_test";
											if (!Directory.Exists(dumpDirectoryName)) { Directory.CreateDirectory(dumpDirectoryName); }
											if (Directory.Exists(dumpDirectoryName))
											{
												string streamStartDate = liveInfoResult.LiveInfo.StartedAt.ToString("yyyy-MM-dd_hh-mm-ss_\"GMT\"");
												string filePath = MultiThreadedDownloaderLib.Utils.GetNumberedFileName(
													$"{dumpDirectoryName}\\{userResult.User.Login}_{streamStartDate}_{chunk.FileName}");
												using (Stream outputStream = File.OpenWrite(filePath))
												{
													using (FileDownloader d = new FileDownloader() { Url = chunk.FileUrl, SkipHeaderRequest = true })
													{
														int errorCode = d.Download(outputStream);
														if (errorCode == 200)
														{
															JObject j = new JObject()
															{
																["id"] = chunk.Id,
																["fileSize"] = outputStream.Length,
																["fileName"] = chunk.FileName,
																["length"] = chunk.Duration,
																["creationDate"] = chunk.CreationDate
															};
															File.WriteAllText($"{filePath}_info.json", j.ToString());

															Console.WriteLine($"OK, {d.DownloadedInLastSession} bytes");
														}
														else
														{
															Console.WriteLine($"Error {errorCode} ({FileDownloader.ErrorCodeToString(errorCode)})");
														}
													}
												}
											}
										}
									}
									catch (Exception ex)
									{
										Console.WriteLine(ex.Message);
									}

									while (chunkList.Count > 50) { chunkList.RemoveAt(0); }

									int elapsed = Environment.TickCount - startTime;
									Console.WriteLine($"Download took {elapsed} milliseconds");
									int delay = 2000 - elapsed;
									Console.WriteLine($"Delay: {delay} milliseconds");
									if (delay > 0)
									{
										Console.WriteLine("Waiting...");
										Thread.Sleep(delay);
									}
								}
							}

							Console.WriteLine("Playlist lost!");
						}
						else
						{
							Console.WriteLine("ERROR! Failed to parse manifest!");
						}
					}
					else
					{
						Console.WriteLine($"ERROR! Can't get playlist manifest! Error code: {manifestResult.ErrorCode}");
					}
				}
				else
				{
					Console.WriteLine("Live stream not found!");
				}

				Console.ReadLine();
			}
		}
	}
}
