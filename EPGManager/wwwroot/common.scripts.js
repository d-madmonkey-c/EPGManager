// Global variables
const availableChannelFilters = {
	searchText: '',
	showNewOnly: false,
	hiddenGroups: new Set()
};

function getAvailableChannelGroups(channel) {
	try {
		const groups = JSON.parse(channel.dataset.groups || '[]');
		if (Array.isArray(groups) && groups.length > 0) {
			return groups
				.map(group => String(group).trim())
				.filter(Boolean);
		}
	} catch (error) {
		console.error('Invalid channel group metadata:', error);
	}

	const effectiveGroup = (channel.dataset.group || '').trim();
	return effectiveGroup ? [effectiveGroup] : [];
}

function populateAvailableChannelHiddenGroups() {
	const hiddenGroupList = document.getElementById('available-channel-hidden-group-list');
	if (!hiddenGroupList) {
		return;
	}

	const groups = new Set();

	document.querySelectorAll('#all-channels .channel-item').forEach(channel => {
		getAvailableChannelGroups(channel).forEach(group => groups.add(group));
	});

	hiddenGroupList.replaceChildren();

	[...groups]
		.sort((left, right) => left.localeCompare(right))
		.forEach(group => {
			const label = document.createElement('label');
			label.className = 'available-channel-hidden-group';

			const checkbox = document.createElement('input');
			checkbox.type = 'checkbox';
			checkbox.value = group;
			checkbox.checked = availableChannelFilters.hiddenGroups.has(group);

			checkbox.addEventListener('change', () => {
				if (checkbox.checked) {
					availableChannelFilters.hiddenGroups.add(group);
				} else {
					availableChannelFilters.hiddenGroups.delete(group);
				}

				applyAvailableChannelFilters();
			});

			label.append(checkbox, document.createTextNode(` ${group}`));
			hiddenGroupList.appendChild(label);
		});
}

function applyAvailableChannelFilters() {
	const searchText = availableChannelFilters.searchText.trim().toLocaleLowerCase();
	const visibleChannelIds = new Set();

	const channels = [...document.querySelectorAll('#all-channels .channel-item')];

	channels.forEach(channel => {
		const name = (channel.dataset.name || '').toLocaleLowerCase();
		const isNew = (channel.dataset.new || 'false') === 'true';
		const channelGroups = getAvailableChannelGroups(channel);

		const matchesSearch =
			searchText.length === 0 ||
			name.includes(searchText);

		const matchesNew =
			!availableChannelFilters.showNewOnly ||
			isNew;

		const matchesHiddenGroups =
			!channelGroups.some(group =>
				availableChannelFilters.hiddenGroups.has(group));

		const visible = matchesSearch && matchesNew && matchesHiddenGroups;
		channel.style.display = visible ? '' : 'none';

		if (visible) {
			visibleChannelIds.add(channel.dataset.id);
		}
	});

	const totalChannelCount = new Set(channels.map(channel => channel.dataset.id)).size;

	document.querySelectorAll('#all-channels .group-title').forEach(groupElement => {
		const visibleChannel = groupElement.querySelector(
			'.group-content .channel-item:not([style*="display: none"])'
		);

		groupElement.style.display = visibleChannel ? '' : 'none';
	});

	const countElement = document.getElementById('available-channel-result-count');
	if (countElement) {
		countElement.textContent = `Showing ${visibleChannelIds.size} of ${totalChannelCount} channels`;
	}

	const emptyElement = document.getElementById('available-channel-empty');
	if (emptyElement) {
		emptyElement.style.display =
			totalChannelCount > 0 && visibleChannelIds.size === 0 ? 'block' : 'none';
	}

	const panelBody = document.querySelector('#all-channels .panel-body');
	if (panelBody) {
		panelBody.style.display =
			totalChannelCount > 0 && visibleChannelIds.size === 0 ? 'none' : '';
	}
}

function clearAvailableChannelFilters() {
	availableChannelFilters.searchText = '';
	availableChannelFilters.showNewOnly = false;
	availableChannelFilters.hiddenGroups.clear();

	const searchInput = document.getElementById('available-channel-search-input');
	if (searchInput) {
		searchInput.value = '';
	}

	const newOnlyInput = document.getElementById('available-channel-new-only');
	if (newOnlyInput) {
		newOnlyInput.checked = false;
	}

	document
		.querySelectorAll('#available-channel-hidden-group-list input[type="checkbox"]')
		.forEach(checkbox => {
			checkbox.checked = false;
		});

	applyAvailableChannelFilters();
}

function initializeAvailableChannelFilters() {
	const searchInput = document.getElementById('available-channel-search-input');
	const newOnlyInput = document.getElementById('available-channel-new-only');
	const clearButton = document.getElementById('available-channel-clear-filters');

	if (searchInput) {
		searchInput.value = availableChannelFilters.searchText;
		searchInput.oninput = () => {
			availableChannelFilters.searchText = searchInput.value;
			applyAvailableChannelFilters();
		};
	}

	if (newOnlyInput) {
		newOnlyInput.checked = availableChannelFilters.showNewOnly;
		newOnlyInput.onchange = () => {
			availableChannelFilters.showNewOnly = newOnlyInput.checked;
			applyAvailableChannelFilters();
		};
	}

	if (clearButton) {
		clearButton.onclick = clearAvailableChannelFilters;
	}

	populateAvailableChannelHiddenGroups();
	applyAvailableChannelFilters();
}

function toggleGroup(groupName) {
	const content = document.getElementById('group-' + groupName);
	const arrow = document.getElementById('arrow-' + groupName);

	if (content.style.display === 'none') {
		content.style.display = 'block';
		arrow.textContent = '▼';
	} else {
		content.style.display = 'none';
		arrow.textContent = '►';
	}
}

function addChannel(id, selectedChannel) {
	var list = document.getElementById('selected-list');

	// Prevent duplicates
	if ([...list.children].some(div => div.querySelector('.channel-gripper').dataset.id === id))
		return;

	// Get the original values from the clicked element
	var name = selectedChannel.dataset.name || '';
	var group = selectedChannel.dataset.group || null;
	var logo = selectedChannel.dataset.logo || null;
	var uri = selectedChannel.dataset.uri || null;

	var channelElement = document.createElement('div');
	channelElement.id = 'selected-' + id;
	channelElement.classList.add('changed','channel-item');

	var gripperElement = document.createElement('div');
	gripperElement.id = 'gripper-' + id;
	gripperElement.classList.add('action-button', 'channel-gripper');
	gripperElement.draggable = true;
	gripperElement.dataset.id = id;
	gripperElement.textContent = '⁞';
	channelElement.appendChild(gripperElement);

	var removeElement = document.createElement('div');
	removeElement.id = 'minus-' + id;
	removeElement.classList.add('action-button', 'channel-remove');
	removeElement.textContent = '←';
	removeElement.onclick = () => removeChannel(channelElement);
	channelElement.appendChild(removeElement);

	var nameElement = document.createElement('div');
	nameElement.className = 'channel-name';
	nameElement.dataset.sourceName = name;
	nameElement.dataset.sourceLogo = logo;
	nameElement.dataset.sourceGroup = group || '';
	nameElement.dataset.id = id;
	nameElement.dataset.name = name;
	nameElement.dataset.logo = logo;
	nameElement.dataset.uri = uri;
	nameElement.dataset.groups = group;
	nameElement.dataset.epgids = '';
	nameElement.textContent = name;
	nameElement.onclick = () => showSettings(nameElement);
	channelElement.appendChild(nameElement);

	list.appendChild(channelElement);
}

function removeChannel(channelElement) {
	channelElement.remove();
}

function showSettings(channelNameElement) {
	var id = channelNameElement.dataset.id;
	var name = channelNameElement.dataset.name;
	var logo = channelNameElement.dataset.logo;
	var groups = channelNameElement.dataset.groups;
	var epgIds = channelNameElement.dataset.epgids;

	var sourceName = channelNameElement.dataset.sourceName || name;
	var sourceLogo = channelNameElement.dataset.sourceLogo || '';
	var sourceGroup = channelNameElement.dataset.sourceGroup || '';

	var contentElement = document.getElementById('settings-content');
	contentElement.querySelector("h3").textContent = name;
	contentElement.querySelector('#settings-default-name').textContent = sourceName;
	contentElement.querySelector('#settings-default-logo').textContent = sourceLogo;
	contentElement.querySelector('#settings-default-group').textContent = sourceGroup;
	contentElement.querySelector('#settings-name').value = name;
	contentElement.querySelector('#settings-logo').value = logo;
	contentElement.querySelector('#settings-groups').value = groups;
	contentElement.querySelector('#epg-mappings').replaceChildren(renderEpgMappings(epgIds));
	contentElement.querySelector('#settings-effective-uri').textContent = channelNameElement.dataset.uri || '';
	const updateEffectiveValues = () => {
		contentElement.querySelector('#settings-effective-name').textContent = contentElement.querySelector('#settings-name').value;
		contentElement.querySelector('#settings-effective-logo').textContent = contentElement.querySelector('#settings-logo').value;
		contentElement.querySelector('#settings-effective-groups').textContent = contentElement.querySelector('#settings-groups').value;
	};
	contentElement.querySelector('#settings-name').oninput = updateEffectiveValues;
	contentElement.querySelector('#settings-logo').oninput = updateEffectiveValues;
	contentElement.querySelector('#settings-groups').oninput = updateEffectiveValues;
	updateEffectiveValues();
	contentElement.querySelector('#settings-addepg').onclick = () => addEpgMapping(channelNameElement);
	contentElement.querySelector('#settings-apply').onclick = () => applySettings(id);
	document.getElementById('settings-placeholder').style.display = 'none';
	contentElement.style.display = 'block';
}

function addM3uUrlRow(name = '', url = '', priority = '') {
	const tbody = document.getElementById('m3u-url-list');
	const priorities = [...tbody.querySelectorAll('[name="priority"]')]
		.map(input => Number(input.value))
		.filter(Number.isFinite);

	if (priority === '') {
		priority = priorities.length ? Math.max(...priorities) + 1 : 1;
	}

	const row = document.createElement('tr');
	row.dataset.id = '';
	row.innerHTML = `
		<td class="column-priority"><input style="width:100%" type="number" name="priority" value="${priority}" min="1" required></td>
		<td class="column-name"><input style="width:100%" type="text" name="name" value="${name}" placeholder="M3U Source Name" required></td>
		<td class="column-url"><input style="width:100%" type="url" name="url" value="${url}" placeholder="M3U Source Url" required></td>
		<td class="column-actions">
			<div class="icon-trash" style="float: left" onclick='removeUrlRow(this)'>
				<div class="trash-lid" style="background-color: #4d4d4d"></div>
				<div class="trash-container" style="background-color: #4d4d4d"></div>
				<div class="trash-line-1"></div>
				<div class="trash-line-2"></div>
				<div class="trash-line-3"></div>
			</div>
		</td>
	`;
	tbody.appendChild(row);
}

function createEpgMappingItem(epg) {
	const item = document.createElement('li');
	item.className = 'epg-mapping-item';
	item.setAttribute('data-sourceId', epg.SourceId);
	item.setAttribute('data-sourceName', epg.SourceName);
	item.setAttribute('data-epgId', epg.EpgId);

	const label = document.createElement('span');
	label.className = 'epg-mapping-label';
	label.textContent = `${epg.SourceName}: ${epg.EpgId}`;
	item.appendChild(label);

	const removeButton = document.createElement('button');
	removeButton.type = 'button';
	removeButton.className = 'epg-mapping-remove';
	removeButton.setAttribute('aria-label', `Remove EPG mapping ${epg.SourceName}: ${epg.EpgId}`);
	removeButton.title = 'Remove EPG mapping';
	removeButton.addEventListener('click', () => removeEpgMapping(removeButton));

	const icon = document.createElement('span');
	icon.className = 'icon-trash';
	icon.setAttribute('aria-hidden', 'true');
	['trash-lid', 'trash-container', 'trash-line-1', 'trash-line-2', 'trash-line-3'].forEach(className => {
		const part = document.createElement('span');
		part.className = className;
		if (className === 'trash-lid' || className === 'trash-container') {
			part.style.backgroundColor = '#4d4d4d';
		}
		icon.appendChild(part);
	});
	removeButton.appendChild(icon);
	item.appendChild(removeButton);
	return item;
}

function renderEpgMappings(epgIds) {
	var epgs;
	try {
		epgs = JSON.parse(epgIds || '[]');
	} catch (ex) {
		console.error(ex);
	}
	if (!Array.isArray(epgs) || epgs.length === 0) {
		const emptyMessage = document.createElement('p');
		emptyMessage.textContent = 'No EPG mappings';
		return emptyMessage;
	}
	const list = document.createElement('ul');
	list.id = 'settings-epgList';
	epgs.forEach(epg => list.appendChild(createEpgMappingItem(epg)));
	return list;
}

function addEpgMapping(channelNameElement) {
	document.getElementById('modal-channel-name').textContent = channelNameElement.dataset.name;
	document.getElementById('modal-channel-id').textContent = channelNameElement.dataset.id;
	document.getElementById('modal-add').onclick = () => addSelectedEpgMapping(channelNameElement);
	loadEpgSources();
	document.getElementById('epg-mapping-modal').style.display = 'block';
}

function closeEpgMappingModal() {
	document.getElementById('epg-mapping-modal').style.display = 'none';
	document.getElementById('modal-channel-name').textContent = "Channel Name";
	document.getElementById('modal-channel-id').textContent = "Channel Id";
	document.getElementById('epg-source-select').value = '';
	document.getElementById('epg-channel-select').innerHTML = '<option value="">Select EPG source first...</option>';
}

async function loadEpgSources() {
	const select = document.getElementById('epg-source-select');

	await fetch('/epg/list-epgSources')
		.then(response => {
			if (!response.ok) {
				throw new Error(`${response.status} (${response.statusText}) : ${response.text()}`);
			}
			return response.json();
		})
		.then(data => {
			select.innerHTML = '<option value="">Select EPG source...</option>';

			data.forEach(source => {
				const option = document.createElement('option');
				option.value = source.id;
				option.textContent = `${source.name} (${source.channelCount} channels)`;
				select.appendChild(option);
			});
		})
		.catch(error => alert('Error loading EPG sources: ' + error.message));
}

function loadEpgChannels() {
	const sourceId = document.getElementById('epg-source-select').value;
	const channelSelect = document.getElementById('epg-channel-select');

	if (!sourceId) {
		channelSelect.innerHTML = '<option value="">Select EPG source first...</option>';
		return;
	}

	fetch(`/epg/list-epgChannels/${sourceId}`)
		.then(response => {
			if (!response.ok) {
				throw new Error(`${response.status} (${response.statusText}) : ${response.text()}`);
			}
			return response.json();
		})
		.then(data => {
			channelSelect.innerHTML = '<option value="">Select channel...</option>';
			data.forEach(channel => {
				const option = document.createElement('option');
				option.value = channel.id;
				option.textContent = channel.name;
				channelSelect.appendChild(option);
			});
		})
		.catch(error => alert('Error loading EPG channels: ' + error.message));
}

function addSelectedEpgMapping(channelNameElement) {
	const sourceSelect = document.getElementById('epg-source-select');
	const channelSelect = document.getElementById('epg-channel-select');
	const sourceId = sourceSelect.value;
	const sourceName = sourceSelect.selectedOptions[0].text.replace(/ \(\d+ channels\)$/, '');
	const channelId = channelSelect.value;

	if (!sourceId || !channelId) {
		alert('Please select both an EPG source and channel.');
		return;
	}

	var epgListElement = document.getElementById('settings-epgList');
	if (!epgListElement) {
		var epgSection = document.getElementById('epg-mappings');
		epgListElement = document.createElement('ul');
		epgListElement.id = 'settings-epgList';
		epgSection.replaceChildren(epgListElement);
	}
	epgListElement.appendChild(createEpgMappingItem({ SourceId: sourceId, SourceName: sourceName, EpgId: channelId }));

	//alert(`Mapped to EPG source ${sourceId} channel ${channelId}`);
	//showSettings(channelNameElement); // Refresh the UI
}

function removeEpgMapping(removeButton) {
	const mapping = removeButton.closest('li');
	if (mapping) {
		mapping.remove();
	}
}

function applySettings(tvgId) {
	var contentElement = document.getElementById('settings-content');
	const item = document.querySelector(`#selected-list .channel-name[data-id="${tvgId}"]`);
	if (item) {
		item.dataset.name = contentElement.querySelector('#settings-name').value;
		item.dataset.logo = contentElement.querySelector('#settings-logo').value;
		item.dataset.groups = contentElement.querySelector('#settings-groups').value;
		var epgListElement = document.getElementById('settings-epgList');
		var epgIds = [];
		if (epgListElement) {
			[...epgListElement.children].forEach(li => {
				var sourceId = li.getAttribute('data-sourceId');
				var sourceName = li.getAttribute('data-sourceName');
				var epgId = li.getAttribute('data-epgId');
				epgIds.push({ SourceId: sourceId, SourceName: sourceName, EpgId: epgId });
			});
		}
		item.dataset.epgids = JSON.stringify(epgIds);
		item.textContent = contentElement.querySelector('#settings-name').value;
		item.classList.add('changed');
	}
}

function addEpgUrlRow(name = '', url = '', priority = '', offset = '') {
	const tbody = document.getElementById('epg-url-list');
	const row = document.createElement('tr');
	row.classList.add('changed');
	row.dataset.id = '';
	row.innerHTML = `
        <td class="column-priority"><input style="width:100%" type="number" name="priority" value="${priority}" min="1" required></td>
        <td class="column-offset"><input style="width:100%" type="number" name="offset" value="${offset}" required></td>
        <td class="column-name"><input style="width:100%" type="text" name="name" value="${name}" placeholder="EPG Source Name" required></td>
        <td class="column-url"><input style="width:100%" type="url" name="url" value="${url}" placeholder="EPG Source Url" required></td>
        <td class="column-actions"><div class="icon-trash" style="float: left" onclick='removeUrlRow(this)'>
			<div class="trash-lid" style="background-color: #4d4d4d"></div>
			<div class="trash-container" style="background-color: #4d4d4d"></div>
			<div class="trash-line-1"></div>
			<div class="trash-line-2"></div>
			<div class="trash-line-3"></div>
		</div></td>
    `;
	tbody.appendChild(row);
}

function removeUrlRow(button) {
	button.closest('tr').remove();
}

async function saveConfig() {
	const m3uUrls = [...document.getElementById('m3u-url-list').querySelectorAll('tr')]?.map(r => ({
		Id: r.dataset.id,
		Priority: Number(r.querySelector('[name="priority"]').value),
		Name: r.querySelector('[name="name"]').value,
		Url: r.querySelector('[name="url"]').value
	})) ?? [];

	const epgUrls = [...document.getElementById('epg-url-list').querySelectorAll("tr")]?.map(r => ({
		Id: r.dataset.id,
		Priority: Number(r.querySelector('[name="priority"]').value),
		Offset: "00:00:00", /*Number(r.querySelector('[name="offset"]').value),*/
		Name: r.querySelector('[name="name"]').value,
		Url: r.querySelector('[name="url"]').value
	})) ?? [];

	const selectedChannels = [...document.querySelectorAll('#selected-list .channel-name')]?.map(c => ({
		Id: c.dataset.id,
		Name: c.dataset.name,
		LogoUri: c.dataset.logo,
		Uri: c.dataset.uri,
		Groups: c.dataset.groups.split(',').map(s => s.trim()).filter(s => s),
		EpgChannelIds: JSON.parse(c.dataset.epgids || '[]').map(cm => ({
			ChannelId: c.dataset.id,
			SourceId: cm.SourceId,
			EpgId: cm.EpgId
		}))
	})) ?? [];

	const payload = {
		M3uUrls: m3uUrls,
		EpgUrls: epgUrls,
		SelectedChannels: selectedChannels,
		ReviewFeedback: collateReviewFeedback()
	};

	fetch("/config", {
		method: "POST",
		headers: {
			"Content-Type": "application/json"
		},
		body: JSON.stringify(payload)
		})
		.then(response => {
			if (response.ok) {
				alert("Configuration saved");
				refreshSourcesTab();
			} else {
				alert("Error saving config");
			}
		})
		.catch(error => alert('Error saving config: ' + error.message));
}

function refreshOutputs() {
	document.getElementById('outputactions').style.display = 'none';
	document.getElementById('progress').style.display = 'block';
	fetch('/refresh', { method: 'POST' })
		.then(response => {
			if (response.ok) {
				return response.json();
			} else {
				throw new Error('Refresh failed');
			}
		})
		.then(data => {
			if (data.success) {
				alert(`Processed ${data.totalChannelCount} total channels.\n${data.newChannelCount} were new, ${data.selectedChannelCount} were selected.\n${data.totalEpgProgrammes} programmes for ${data.totalEpgChannels} across ${data.totalSources} sources were processed.`);
				document.getElementById('progress').style.display = 'none';
				document.getElementById('outputactions').style.display = 'block';
				// Reload the page to show updated channels
				//window.location.reload();
				refreshChannelsTab();
				refreshPreviewTab();
			} else {
				alert('Error: ' + data.error);
			}
		})
		.catch(error => alert('Error triggering refresh: ' + error.message));
}

function refreshAllM3u() {
	if (!confirm('Download latest M3U file and update available channels? New channels will be highlighted.')) {
		return;
	}

	fetch('/refresh-m3u', { method: 'POST' })
		.then(response => response.json())
		.then(data => {
			if (data.success) {
				alert(data.message);
				// Reload the page to show updated channels
				//window.location.reload();
				refreshChannelsTab();
			} else {
				alert('Error: ' + data.error);
			}
		})
		.catch(error => alert('Error refreshing M3U: ' + error.message));
}

function refreshM3u(id) {
	fetch(`/refresh/m3u/${id}`, { method: 'POST' })
		.then(response => response.json())
		.then(data => {
			if (data.success) {
				alert(data.message);
			} else {
				alert('Error: ' + data.error);
			}
		})
		.catch(error => alert('Error refreshing M3U: ' + error.message));
}

function refreshEpg(id) {
	fetch(`/refresh/epg/${id}`, { method: 'POST' })
		.then(response => response.json())
		.then(data => {
			if (data.success) {
				alert(data.message);
			} else {
				alert('Error: ' + data.error);
			}
		})
		.catch(error => alert('Error refreshing EPG: ' + error.message));
}

function refreshAllEpg() {
	if (!confirm('Download and cache all EPG files? This may take a moment.')) {
		return;
	}

	fetch('/refresh-epg', { method: 'POST' })
		.then(response => response.json())
		.then(data => {
			if (data.success) {
				alert(data.message);
			} else {
				alert('Error: ' + data.error);
			}
		})
		.catch(error => alert('Error refreshing EPG: ' + error.message));
}

function refreshSourcesTab() {
	fetch('/sourceConfigContent', { method: 'GET' })
		.then(response => {
			if (response.ok) {
				return response.text();
			} else {
				throw new Error('Refresh failed');
			}
		})
		.then(data => {
			document.getElementById("tab-content-sources").innerHTML = data;
		})
		.catch(error => alert('Error refreshing Sources: ' + error.message));
}

function refreshChannelsTab() {
	fetch('/channelConfigContent', { method: 'GET' })
		.then(response => {
			if (response.ok) {
				return response.text();
			} else {
				throw new Error('Refresh failed');
			}
		})
		.then(data => {
			document.getElementById("tab-content-channels").innerHTML = data;
			initializeAvailableChannelFilters();
		})
		.catch(error => alert('Error refreshing Channels: ' + error.message));
}

// --- DRAG AND DROP FOR SELECTED CHANNELS ---

let draggedItem = null;

document.addEventListener('DOMContentLoaded', () => {
	initializeAvailableChannelFilters();

	const list = document.getElementById('selected-list');

	list.addEventListener('dragstart', (e) => {
		if (!e.target.classList.contains('channel-gripper')) return;
		draggedItem = e.target.parentElement;
		e.dataTransfer.effectAllowed = 'move';
		e.dataTransfer.setData('text/plain', draggedItem.dataset.id);
		e.dataTransfer.setDragImage(draggedItem, 10, 10);
		draggedItem.classList.add('dragging');
	});

	list.addEventListener('dragend', (e) => {
		if (draggedItem) {
			draggedItem.classList.remove('dragging');
			draggedItem = null;
		}
	});

	list.addEventListener('dragover', (e) => {
		e.preventDefault();
		const target = e.target.closest('.channel-item');
		if (!target || target === draggedItem) return;

		const rect = target.getBoundingClientRect();
		const midpoint = rect.top + rect.height / 2;

		if (e.clientY < midpoint) {
			target.parentNode.insertBefore(draggedItem, target);
		}
		else {
			target.parentNode.insertBefore(draggedItem, target.nextSibling);
		}
	});
});
