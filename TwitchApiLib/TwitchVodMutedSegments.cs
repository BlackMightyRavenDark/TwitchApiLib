using System;
using System.Collections.Generic;
using System.Linq;
using static TwitchApiLib.TwitchVodChunk;

namespace TwitchApiLib
{
	public class TwitchVodMutedSegments
	{
		public List<string> SegmentListFormatted { get; }
		public TimeSpan TotalDuration { get; private set; }
		public List<TwitchVodChunk> Chunks { get; }
		public List<List<TwitchVodChunk>> Segments { get; }

		public TwitchVodMutedSegments()
		{
			SegmentListFormatted = new List<string>();
			TotalDuration = TimeSpan.Zero;
			Chunks = new List<TwitchVodChunk>();
			Segments = new List<List<TwitchVodChunk>>();
		}

		public void CalculateTotalDuration()
		{
			TotalDuration = TimeSpan.Zero;

			foreach (List<TwitchVodChunk> list in Segments)
			{
				foreach (TwitchVodChunk chunk in list)
				{
					TimeSpan chunkDurationTimeSpan = TimeSpan.FromSeconds(chunk.Duration);
					TotalDuration = TotalDuration.Add(chunkDurationTimeSpan);
				}
			}
		}

		public void Parse(bool showChunkCount = false)
		{
			SegmentListFormatted.Clear();
			Chunks.Clear();
			foreach (List<TwitchVodChunk> list in Segments)
			{
				string t = SegmentToString(list, showChunkCount);
				SegmentListFormatted.Add(t);

				foreach (TwitchVodChunk chunk in list)
				{
					Chunks.Add(chunk);
				}
			}
		}

		private static string SegmentToString(List<TwitchVodChunk> segment, bool showChunkCount = false)
		{
			if (segment != null && segment.Count > 0)
			{
				int chunkCount = segment.Count;
				double segmentDuration = chunkCount > 1 ? segment.Sum(item => item.Duration) : segment[0].Duration;
				TimeSpan start = TimeSpan.FromSeconds(segment[0].Offset);
				TimeSpan end = TimeSpan.FromSeconds(segment[0].Offset + segmentDuration);
				string t = $"{start:hh':'mm':'ss} - {end:hh':'mm':'ss}";
				if (showChunkCount) { t += $" ({chunkCount} chunks)"; }
				return t;
			}

			return "<empty segment>";
		}

		public static TwitchVodMutedSegments ParseMutedSegments(IEnumerable<TwitchVodChunk> chunks)
		{
			TwitchVodMutedSegments result = new TwitchVodMutedSegments();
			List<TwitchVodChunk> segmentList = null;
			foreach (TwitchVodChunk chunk in chunks)
			{
				if (chunk.GetState() != TwitchVodChunkState.NotMuted)
				{
					if (segmentList == null)
					{
						segmentList = new List<TwitchVodChunk>();
					}

					TwitchVodChunk chunkCopy = new TwitchVodChunk(chunk);
					result.Chunks.Add(chunkCopy);
					segmentList.Add(chunkCopy);
				}
				else if (segmentList != null)
				{
					result.Segments.Add(segmentList);
					segmentList = null;
				}
			}

			if (segmentList != null)
			{
				result.Segments.Add(segmentList);
			}

			return result;
		}

		public void Clear()
		{
			Segments.Clear();
			Chunks.Clear();
			SegmentListFormatted.Clear();
			TotalDuration = TimeSpan.Zero;
		}

		public override string ToString()
		{
			string t = string.Empty;
			if (SegmentListFormatted.Count > 0)
			{
				foreach (string segment in SegmentListFormatted)
				{
					t += segment + Environment.NewLine;
				}
			}
			return t;
		}
	}
}
