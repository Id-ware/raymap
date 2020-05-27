﻿
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System.Linq;
using OpenSpace;
using System.IO;
using UnityEngine;
using OpenSpace.Loader;

class PS1ActorSelectionDropdown : AdvancedDropdown {
	public string selection = null;
	public Settings.Mode mode;
	public string name;
	public string[] actors;
	//public SerializedProperty property;

	public PS1ActorSelectionDropdown(AdvancedDropdownState state, Settings.Mode mode) : base(state) {
		this.mode = mode;
		minimumSize = new UnityEngine.Vector2(50, 300f);
	}

	protected override AdvancedDropdownItem BuildRoot() {
		var root = new AdvancedDropdownItem(name);
		if (OpenSpace.PS1.PS1GameInfo.Games.ContainsKey(mode)) {
			actors = OpenSpace.PS1.PS1GameInfo.Games[mode].actors;
			for (int i = 0; i < actors.Length; i++) {
				Add(root, actors[i], i);
			}
		}

		return root;
	}

	protected void Add(AdvancedDropdownItem parent, string actorName, int id) {
		parent.AddChild(new AdvancedDropdownItem(actorName) {
			id = id
		});
	}

	protected override void ItemSelected(AdvancedDropdownItem item) {
		base.ItemSelected(item);
		if (item.children.Count() == 0 && actors != null && actors.Length > item.id) {
			selection = actors[item.id];
			//property.stringValue = selection;
		}
	}
}
