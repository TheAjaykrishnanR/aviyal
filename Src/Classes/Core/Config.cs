using System;
using System.Text.Json.Nodes;
using System.Collections.Generic;
using System.Linq;

public class Config : IJson<Config>
{
	public string layout { get; set; } = "dwindle";

	// margins
	public int left { get; set; } = 5;
	public int top { get; set; } = 5;
	public int right { get; set; } = 5;
	public int bottom { get; set; } = 5;

	public int inner { get; set; } = 5;
	public int workspaces { get; set; } = 9;
	public string floatingWindowSize { get; set; } = "800x400";
	public bool workspaceAnimations = false;
	public int workspaceAnimationsDuration = 500; // milliseconds
	public string workspaceAnimationsDirection = "horizontal";
	public int serverPort = 6969;

	public List<WindowRule> rules = new();
	public List<Keymap> keymaps = new() {
		// focus workspaces
		new() { keys= [VK.LCONTROL, VK.LSHIFT, VK.L], command= COMMAND.FOCUS_NEXT_WORKSPACE },
		new() { keys= [VK.LCONTROL, VK.LSHIFT, VK.H], command= COMMAND.FOCUS_PREVIOUS_WORKSPACE },
		// close window
		new() { keys= [VK.LCONTROL, VK.LSHIFT, VK.X], command= COMMAND.CLOSE_FOCUSED_WINDOW },
		// toggle floating window
		new() { keys= [VK.LCONTROL, VK.LSHIFT, VK.Z], command= COMMAND.TOGGLE_FLOATING_WINDOW },
		// focus window
		new() { keys= [VK.LCONTROL, VK.H], command= COMMAND.FOCUS_LEFT_WINDOW },
		new() { keys= [VK.LCONTROL, VK.K], command= COMMAND.FOCUS_TOP_WINDOW },
		new() { keys= [VK.LCONTROL, VK.L], command= COMMAND.FOCUS_RIGHT_WINDOW },
		new() { keys= [VK.LCONTROL, VK.J], command= COMMAND.FOCUS_BOTTOM_WINDOW },
		// shift focused window (left/right)
		new() { keys= [VK.LMENU, VK.L], command= COMMAND.SHIFT_FOCUSED_WINDOW_RIGHT },
		new() { keys= [VK.LMENU, VK.H], command= COMMAND.SHIFT_FOCUSED_WINDOW_LEFT },
		// shift focused window (workspace)
		new() { keys= [VK.LMENU, VK.LSHIFT, VK.L], command= COMMAND.SHIFT_WINDOW_NEXT_WORKSPACE },
		new() { keys= [VK.LMENU, VK.LSHIFT, VK.H], command= COMMAND.SHIFT_WINDOW_PREVIOUS_WORKSPACE },
		// jump to numbered workspace
		new() { keys= [VK.LMENU, VK.NUM1], command= COMMAND.FOCUS_WORKSPACE_1 },
		new() { keys= [VK.LMENU, VK.NUM2], command= COMMAND.FOCUS_WORKSPACE_2 },
		new() { keys= [VK.LMENU, VK.NUM3], command= COMMAND.FOCUS_WORKSPACE_3 },
		new() { keys= [VK.LMENU, VK.NUM4], command= COMMAND.FOCUS_WORKSPACE_4 },
		new() { keys= [VK.LMENU, VK.NUM5], command= COMMAND.FOCUS_WORKSPACE_5 },
		new() { keys= [VK.LMENU, VK.NUM6], command= COMMAND.FOCUS_WORKSPACE_6 },
		new() { keys= [VK.LMENU, VK.NUM7], command= COMMAND.FOCUS_WORKSPACE_7 },
		new() { keys= [VK.LMENU, VK.NUM8], command= COMMAND.FOCUS_WORKSPACE_8 },
		new() { keys= [VK.LMENU, VK.NUM9], command= COMMAND.FOCUS_WORKSPACE_9 },

		new() { keys= [VK.LCONTROL, VK.LSHIFT, VK.R], command= COMMAND.RESTART },
		new() { keys= [VK.LCONTROL, VK.LSHIFT, VK.U], command= COMMAND.UPDATE },
	};

	public string ToJson()
	{
		JsonObject j = new()
		{
			["layout"] = layout,
			["left"] = left,
			["top"] = top,
			["right"] = right,
			["bottom"] = bottom,
			["inner"] = inner,
			["workspaces"] = workspaces,
			["workspaceAnimations"] = workspaceAnimations.ToString(),
			["workspaceAnimationsDuration"] = workspaceAnimationsDuration.ToString(),
			["workspaceAnimationsDirection"] = workspaceAnimationsDirection.ToString(),
			["floatingWindowSize"] = floatingWindowSize,
			["serverPort"] = serverPort,
			["rules"] = new JsonArray(
				rules.Select(
					rule => new JsonObject()
					{
						["type"] = rule.type,
						["method"] = rule.method,
						["identifierType"] = rule.identifierType,
						["identifier"] = rule.identifier
					}
				).ToArray()
			),
			["keymaps"] = new JsonArray(
				keymaps.Select(
					keymap => new JsonObject()
					{
						["keys"] = new JsonArray(
							keymap.keys.Select(key => (JsonNode)key.ToString()).ToArray()
						),
						["command"] = keymap.command.ToString(),
						["arguments"] = new JsonArray(keymap.arguments.Select(arg => (JsonNode)arg).ToArray()),
					}
				).ToArray()
			)
		};
		return j.ToString();
	}

	public static Config FromJson(string json)
	{
		JsonNode node = JsonNode.Parse(json);

		Config config = new();
		config.layout = node["layout"].ToString();
		config.inner = Convert.ToInt32(node["inner"].ToString());
		config.left = Convert.ToInt32(node["left"].ToString());
		config.top = Convert.ToInt32(node["top"].ToString());
		config.right = Convert.ToInt32(node["right"].ToString());
		config.bottom = Convert.ToInt32(node["bottom"].ToString());
		config.workspaces = Convert.ToInt32(node["workspaces"].ToString());
		config.workspaceAnimations =
		node["workspaceAnimations"].ToString() switch
		{
			"true" => true,
			"false" => false
		};
		config.workspaceAnimationsDuration =
			Convert.ToInt32(node["workspaceAnimationsDuration"].ToString());
		config.workspaceAnimationsDirection =
			node["workspaceAnimationsDirection"].ToString();
		config.floatingWindowSize = node["floatingWindowSize"].ToString();
		config.serverPort = Convert.ToInt32(node["serverPort"].ToString());

		config.rules = new();
		JsonArray _rules = node["rules"].AsArray();
		_rules.ToList().ForEach(_rule =>
		{
			WindowRule rule = new();

			rule.type = _rule["type"].ToString();
			rule.method = _rule["method"].ToString();
			rule.identifierType = _rule["identifierType"].ToString();
			rule.identifier = _rule["identifier"].ToString();

			config.rules.Add(rule);
		});

		config.keymaps = new();
		JsonArray _keymaps = node["keymaps"].AsArray();
		_keymaps.ToList().ForEach(_keymap =>
		{
			Keymap keymap = new();

			// keys
			JsonArray _keys = _keymap["keys"].AsArray();
			_keys.ToList().ForEach(_key =>
			{
				Enum.TryParse<VK>(_key.ToString(), true, out VK vkKey);
				keymap.keys.Add(vkKey);
			});
			// command
			string _command = _keymap["command"].ToString();
			Enum.TryParse<COMMAND>(_command, true, out keymap.command);
			// arguments
			JsonArray _arguments = _keymap["arguments"].AsArray();
			_arguments.ToList().ForEach(_arg =>
			{
				keymap.arguments.Add(_arg.ToString());
			});

			config.keymaps.Add(keymap);
		});

		return config;
	}
}

public class WindowRule
{
	public string type; // ignore, notiling, startfloating
	public string method; // equals, contains
	public string identifierType; // windowProcess, windowTitle, windowClass
	public string identifier; // search string
}

