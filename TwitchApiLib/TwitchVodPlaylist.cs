using System;
using System.Collections.Generic;
using System.Globalization;

namespace TwitchApiLib
{
	public class TwitchVodPlaylist
	{
		public string PlaylistRaw { get; }
		public string PlaylistUrl { get; }
		public string StreamRootUrl { get; }
		public TwitchVodPlaylistManifestItem Information { get; }
		public List<TwitchVodChunk> ChunkList { get; }
		public int FirstChunkId { get; private set; }
		public int Count => ChunkList.Count;
		public TwitchVodMutedSegments MutedSegments => GetMutedSegments();
		public string this[int id] => StreamRootUrl + ChunkList[id].FileName;

		private bool _isParsed = false;

		public TwitchVodPlaylist(string playlistRaw, string playlistUrl,
			TwitchVodPlaylistManifestItem information)
		{
			PlaylistRaw = playlistRaw;
			if (information != null)
			{
				PlaylistUrl = !string.IsNullOrEmpty(information.PlaylistUrl) &&
					!string.IsNullOrWhiteSpace(information.PlaylistUrl) ?
					information.PlaylistUrl : playlistUrl;
				if (!string.IsNullOrEmpty(PlaylistUrl) && !string.IsNullOrWhiteSpace(PlaylistUrl))
				{
					int n = PlaylistUrl.LastIndexOf('/');
					if (n > 0) { StreamRootUrl = PlaylistUrl.Substring(0, n + 1); }
				}
				else
				{
					StreamRootUrl = null;
				}
			}
			else if (!string.IsNullOrEmpty(playlistUrl) && !string.IsNullOrWhiteSpace(playlistUrl))
			{
				PlaylistUrl = playlistUrl;
				int n = playlistUrl.LastIndexOf("/");
				if (n > 0)
				{
					StreamRootUrl = playlistUrl.Substring(0, n + 1);
				}
			}

			Information = information;
			ChunkList = new List<TwitchVodChunk>();
		}

		public TwitchVodChunk GetChunk(int id)
		{
			return ChunkList[id];
		}

		public int Parse(bool anyway = false)
		{
			if (anyway || !_isParsed)
			{
				_isParsed = true;

				ChunkList.Clear();

				if (string.IsNullOrEmpty(PlaylistRaw) || string.IsNullOrWhiteSpace(PlaylistRaw))
				{
					return 0;
				}

				string[] strings = PlaylistRaw.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.None);
				if (strings.Length > 1)
				{
					NumberFormatInfo numberFormatInfo = new NumberFormatInfo() { NumberDecimalSeparator = "." };

					double chunkOffsetInPlaylist = 0.0;
					DateTime chunkCreationDate = DateTime.MinValue;
					double previousChunkLength = 0.0;
					int foundChunks = 0;
					for (int i = 0; i < strings.Length; ++i)
					{
						string[] splitted = strings[i].Split(':');
						if (splitted[0] == "#EXT-X-MEDIA-SEQUENCE" && int.TryParse(splitted[1], out int firstId))
						{
							FirstChunkId = firstId;
							continue;
						}
						else if (splitted[0] == "#EXTINF")
						{
							string[] chunkLengthSplitted = splitted[1].Split(',');

							double chunkLength = double.TryParse(chunkLengthSplitted[0], NumberStyles.Any,
								numberFormatInfo, out double sec) ? sec : 0.0;
							bool isLiveStreamchunk = chunkLengthSplitted.Length > 1 && chunkLengthSplitted[1] == "live";
							if (ExtractProgramDateTimeFromString(strings[i - 1], out DateTime dateTime))
							{
								chunkCreationDate = dateTime;
							}
							else if (previousChunkLength > 0.0)
							{
								chunkCreationDate += TimeSpan.FromSeconds(previousChunkLength);
							}

							int chunkId = FirstChunkId + foundChunks;
							bool isUrl = strings[i + 1].StartsWith("http");
							string fileUrl = isUrl ? strings[i + 1] : $"{StreamRootUrl}/{strings[i + 1]}";
							string fileName = isUrl ? $"{chunkId}.ts" : strings[i + 1];

							TwitchVodChunk chunk = new TwitchVodChunk(chunkId, fileName, fileUrl,
								chunkOffsetInPlaylist, chunkLength, chunkCreationDate, isLiveStreamchunk);
							ChunkList.Add(chunk);

							if (chunkLength > 0.0)
							{
								chunkOffsetInPlaylist += chunkLength;
							}
							previousChunkLength = chunkLength;
							foundChunks++;
							i++;
						}
					}
				}
			}

			return ChunkList.Count;
		}

		private static bool ExtractProgramDateTimeFromString(string inputString, out DateTime dateTime)
		{
			if (inputString.StartsWith("#EXT-X-PROGRAM-DATE-TIME:"))
			{
				string[] splitted = inputString.Split(new char[] { ':' }, 2);
				if (DateTime.TryParseExact(splitted[1], "yyyy-MM-ddTHH:mm:ss.fffZ",
					null, DateTimeStyles.AdjustToUniversal, out dateTime))
				{
					return true;
				}
			}

			dateTime = DateTime.MaxValue;
			return false;
		}

		public TwitchVodMutedSegments GetMutedSegments(bool showChunkCount = false)
		{
			if (Parse() > 0)
			{
				TwitchVodMutedSegments mutedSegments =
					TwitchVodMutedSegments.ParseMutedSegments(ChunkList);
				mutedSegments.Parse(showChunkCount);
				mutedSegments.CalculateTotalDuration();
				return mutedSegments;
			}

			return null;
		}

		public override string ToString()
		{
			string t = string.Empty;
			if (Count > 0)
			{
				for (int i = 0; i < ChunkList.Count; ++i) { t += this[i] + Environment.NewLine; }
			}
			return t;
		}
	}
}
