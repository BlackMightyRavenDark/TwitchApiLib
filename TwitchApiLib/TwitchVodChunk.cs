using System;

namespace TwitchApiLib
{
	public class TwitchVodChunk
	{
		public string FileName { get; private set; }
		public double Offset { get; }
		public double Duration { get; }

		public enum TwitchVodChunkState { NotMuted, Muted, Unmuted };

		public TwitchVodChunk(string fileName, double offset, double duration)
		{
			FileName = fileName;
			Offset = offset;
			Duration = duration;
		}

		public TwitchVodChunk(TwitchVodChunk chunk) :
			this(chunk.FileName, chunk.Offset, chunk.Duration)
		{ }

		public string ExtractNumberFromFileName()
		{
			if (string.IsNullOrEmpty(FileName) || string.IsNullOrWhiteSpace(FileName))
			{
				return null;
			}

			int n = FileName.IndexOf(
				GetState() == TwitchVodChunkState.NotMuted ? ".ts" : "-",
				StringComparison.OrdinalIgnoreCase);
			return FileName.Substring(0, n);
		}

		public TwitchVodChunkState GetState()
		{
			if (FileName.EndsWith("-muted.ts"))
			{
				return TwitchVodChunkState.Muted;
			}
			else if (FileName.EndsWith("-unmuted.ts"))
			{
				return TwitchVodChunkState.Unmuted;
			}
			return TwitchVodChunkState.NotMuted;
		}

		public void SetState(TwitchVodChunkState state)
		{
			switch (state)
			{
				case TwitchVodChunkState.Muted:
					FileName = ExtractNumberFromFileName() + "-muted.ts";
					break;

				case TwitchVodChunkState.Unmuted:
					FileName = ExtractNumberFromFileName() + "-unmuted.ts";
					break;

				case TwitchVodChunkState.NotMuted:
					FileName = ExtractNumberFromFileName() + ".ts";
					break;
			}
		}

		public TwitchVodChunkState NextState()
		{
			switch (GetState())
			{
				case TwitchVodChunkState.Muted:
					SetState(TwitchVodChunkState.NotMuted);
					return TwitchVodChunkState.NotMuted;

				case TwitchVodChunkState.NotMuted:
					SetState(TwitchVodChunkState.Unmuted);
					return TwitchVodChunkState.Unmuted;

				case TwitchVodChunkState.Unmuted:
					SetState(TwitchVodChunkState.Muted);
					return TwitchVodChunkState.Muted;
			}
			return TwitchVodChunkState.NotMuted;
		}
	}
}
