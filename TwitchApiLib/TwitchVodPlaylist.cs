using System;
using System.Collections.Generic;
using System.Globalization;

namespace TwitchApiLib
{
	public class TwitchVodPlaylist
	{
		public string PlaylistRaw { get; }
		public string PlaylistUrl { get; }
		public string StreamRoot { get; }
		public int Count => _chunks.Count;

		private List<TwitchVodChunk> _chunks = new List<TwitchVodChunk>();

		public string this[int id] { get { return StreamRoot + _chunks[id].FileName; } }

		public TwitchVodPlaylist(string playlistRaw, string playlistUrl)
		{
			PlaylistRaw = playlistRaw;
			PlaylistUrl = playlistUrl;
			StreamRoot = playlistUrl.Substring(0, playlistUrl.LastIndexOf('/') + 1);
		}

		public TwitchVodChunk GetChunk(int id)
		{
			return _chunks[id];
		}

		public int Parse()
		{
			_chunks.Clear();

			if (string.IsNullOrEmpty(PlaylistRaw) || string.IsNullOrWhiteSpace(PlaylistUrl))
			{
				return 0;
			}

			string[] strings = PlaylistRaw.Split('\n');
			int stringId = FindFirstChunkStringId(strings);
			if (stringId >= 0)
			{
				double offset = 0.0;
				for (; stringId < strings.Length; stringId += 2)
				{
					string[] splitted = strings[stringId].Split(':');
					if (splitted[0] != "#EXTINF") { continue; }
					string[] lengthSplitted = splitted[1].Split(',');

					NumberFormatInfo numberFormatInfo = new NumberFormatInfo() { NumberDecimalSeparator = "." };
					double chunkLength = double.TryParse(lengthSplitted[0], NumberStyles.Any,
						numberFormatInfo, out double d) ? d : 0.0;

					TwitchVodChunk chunk = new TwitchVodChunk(strings[stringId + 1], offset, chunkLength);
					_chunks.Add(chunk);

					offset += chunkLength;
				}
			}

			return _chunks.Count;
		}

		private int FindFirstChunkStringId(string[] strings)
		{
			for (int i = 0; i < 20 && i < strings.Length; ++i)
			{
				if (strings[i].StartsWith("#EXTINF:")) { return i; }
			}

			return -1;
		}

		public override string ToString()
		{
			string t = string.Empty;
			if (Count > 0)
			{
				for (int i = 0; i < _chunks.Count; ++i) { t += this[i] + Environment.NewLine; }
			}
			return t;
		}
	}
}

