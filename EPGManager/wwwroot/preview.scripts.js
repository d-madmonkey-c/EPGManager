function refreshPreviewTab() {
	fetch('/previewContent', { method: 'GET' })
		.then(response => {
			if (response.ok) {
				return response.text();
			} else {
				throw new Error('Refresh failed');
			}
		})
		.then(data => {
			document.getElementById("tab-content-preview").innerHTML = data;
		})
		.catch(error => alert('Error refreshing Preview: ' + error.message));
}
