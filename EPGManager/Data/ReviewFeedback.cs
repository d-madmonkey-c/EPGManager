using System;

namespace EPGManager.Data;

public class ReviewFeedbackList : List<ReviewFeedback>
{
	public ReviewFeedbackList() { }

	public ReviewFeedbackList(IEnumerable<ReviewFeedback> items) : base(items) { }

	public double GetAccuracyForSource(string sourceId)
	{
		var feedbacks = this.Where(rf => rf.SourceId == sourceId).ToList();
		if (feedbacks.Count == 0) return 0.0;
		return feedbacks.Count(rf => rf.IsAccurate) / (double)feedbacks.Count;
	}
}

public class ReviewFeedback
{
	public string ChannelId { get; set; } = string.Empty;
	public string SourceId { get; set; } = string.Empty;
	public bool IsAccurate { get; set; } = false;
	public double Offset { get; set; } = 0.0;
}
