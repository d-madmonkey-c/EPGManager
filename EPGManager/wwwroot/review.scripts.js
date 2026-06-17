function refreshReviewTab() {
	fetch('/reviewContent', { method: 'GET' })
		.then(response => {
			if (response.ok) {
				return response.text();
			} else {
				throw new Error('Refresh failed');
			}
		})
		.then(data => {
			document.getElementById("tab-content-review").innerHTML = data;
		})
		.catch(error => alert('Error refreshing Review: ' + error.message));
}

function updateAccuracy(source) {
	var sourceId = source.closest("tr").dataset.sourceid;
	const coveredChannels = document.getElementById('review-table').querySelectorAll(`tr[data-sourceid=${sourceId}`);
	var totalChannels = coveredChannels.length;
	//var accurateChannels = coveredChannels.querySelectorAll('input[name="accurate"]').filter(i => i.checked);
	var accurateChannels = [...coveredChannels].filter(r => r.querySelector('input[name="accurate"]:checked')).length;
	//var accurateChannels = 1;
	var accuracyElement = document.getElementById(`review-epg-${sourceId}-accuracy`);
	accuracyElement.innerText = `${((accurateChannels / totalChannels) * 100).toFixed(2)}%`;
}

function collateReviewFeedback() {
	const feedback = [...document.getElementById('review-table').querySelectorAll("tr[data-channelid]:not([data-channelid=\"\"])")].map(r => ({
		ChannelId: r.dataset.channelid,
		SourceId: r.dataset.sourceid,
		IsAccurate: r.querySelector('input[name="accurate"]:checked') !== null,
		Offset: Number(r.querySelector('[name="offset"]').value ?? 0)
	}));
	return feedback;
}