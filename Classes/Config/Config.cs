using System;
using System.Text.Json.Nodes;
using System.Collections.Generic;
using System.Linq;

public class Config : IJson<Config>
{
	public string layout { get; set; } = "dwindle";
	public int outer { get; set; } = 5;
	public int inner { get; set; } = 5;
	public int workspaces { get; set; } = 9;
	public string floatingWindowSize { get; set; } = "800x400";
	public List<Keymap> keymaps = new() {
		// focus workspaces
		new() { keys= [VK.LCONTROL, VK.LSHIFT, VK.L], command= COMMAND.FOCUS_NEXT_WORKSPACE },
		new() { keys= [VK.LCONTROL, VK.LSHIFT, VK.H], command= COMMAND.FOCUS_PREVIOUS_WORKSPACE },
		// close window
		new() { keys= [VK.LCONTROL, VK.LSHIFT, VK.X], command= COMMAND.CLOSE_FOCUSED_WINDOW},
		// focus window
		new() { keys= [VK.LCONTROL, VK.H], command= COMMAND.FOCUS_LEFT_WINDOW},
		new() { keys= [VK.LCONTROL, VK.K], command= COMMAND.FOCUS_TOP_WINDOW},
		new() { keys= [VK.LCONTROL, VK.L], command= COMMAND.FOCUS_RIGHT_WINDOW
		},
		new() { keys= [VK.LCONTROL, VK.J], command= COMMAND.FOCUS_BOTTOM_WINDOW},
	};

	public string ToJson()
	{
		JsonObject j = new()
		{
			["layout"] = layout,
			["outer"] = outer,
			["inner"] = inner,
			["workspaces"] = workspaces,
			["floatingWindowSize"] = floatingWindowSize,
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
		config.outer = Convert.ToInt32(node["outer"].ToString());
		config.workspaces = Convert.ToInt32(node["workspaces"].ToString());
		config.floatingWindowSize = node["floatingWindowSize"].ToString();

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


