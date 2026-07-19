using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

public class Config : IJson<Config>
{
    public string loglevel { get; set; } = "low";
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
    public List<Keymap> keymaps = new()
    {
        // focus workspaces
        new() { keys = [VK.LCONTROL, VK.LSHIFT, VK.L], command = COMMAND.FOCUS_NEXT_WORKSPACE },
        new() { keys = [VK.LCONTROL, VK.LSHIFT, VK.H], command = COMMAND.FOCUS_PREVIOUS_WORKSPACE },
        // close window
        new() { keys = [VK.LCONTROL, VK.LSHIFT, VK.X], command = COMMAND.CLOSE_FOCUSED_WINDOW },
        // toggle floating window
        new() { keys = [VK.LCONTROL, VK.LSHIFT, VK.Z], command = COMMAND.TOGGLE_FLOATING_WINDOW },
        // toggle stacked window
        new() { keys = [VK.LCONTROL, VK.LSHIFT, VK.S], command = COMMAND.TOGGLE_STACKED_WINDOW },
        // focus window
        new() { keys = [VK.LCONTROL, VK.H], command = COMMAND.FOCUS_LEFT_WINDOW },
        new() { keys = [VK.LCONTROL, VK.K], command = COMMAND.FOCUS_TOP_WINDOW },
        new() { keys = [VK.LCONTROL, VK.L], command = COMMAND.FOCUS_RIGHT_WINDOW },
        new() { keys = [VK.LCONTROL, VK.J], command = COMMAND.FOCUS_BOTTOM_WINDOW },
        // shift focused window (left/right)
        new() { keys = [VK.LMENU, VK.L], command = COMMAND.SHIFT_FOCUSED_WINDOW_RIGHT },
        new() { keys = [VK.LMENU, VK.H], command = COMMAND.SHIFT_FOCUSED_WINDOW_LEFT },
        // shift focused window (workspace)
        new()
        {
            keys = [VK.LMENU, VK.LSHIFT, VK.L],
            command = COMMAND.SHIFT_WINDOW_NEXT_WORKSPACE,
        },
        new()
        {
            keys = [VK.LMENU, VK.LSHIFT, VK.H],
            command = COMMAND.SHIFT_WINDOW_PREVIOUS_WORKSPACE,
        },
        new()
        {
            keys = [VK.LCONTROL, VK.LSHIFT, VK.M],
            command = COMMAND.TOGGLE_FOCUSED_WINDOW_MAXIMIZATION,
        },
        new()
        {
            keys = [VK.LCONTROL, VK.LSHIFT, VK.NUM0],
            command = COMMAND.MINIMIZE_FOCUSED_WINDOW,
        },
        new() { keys = [VK.LMENU, VK.LSHIFT, VK.NUM1], command = COMMAND.SHIFT_WINDOW_WORKSPACE_1 },
        new() { keys = [VK.LMENU, VK.LSHIFT, VK.NUM2], command = COMMAND.SHIFT_WINDOW_WORKSPACE_2 },
        new() { keys = [VK.LMENU, VK.LSHIFT, VK.NUM3], command = COMMAND.SHIFT_WINDOW_WORKSPACE_3 },
        new() { keys = [VK.LMENU, VK.LSHIFT, VK.NUM4], command = COMMAND.SHIFT_WINDOW_WORKSPACE_4 },
        new() { keys = [VK.LMENU, VK.LSHIFT, VK.NUM5], command = COMMAND.SHIFT_WINDOW_WORKSPACE_5 },
        new() { keys = [VK.LMENU, VK.LSHIFT, VK.NUM6], command = COMMAND.SHIFT_WINDOW_WORKSPACE_6 },
        new() { keys = [VK.LMENU, VK.LSHIFT, VK.NUM7], command = COMMAND.SHIFT_WINDOW_WORKSPACE_7 },
        new() { keys = [VK.LMENU, VK.LSHIFT, VK.NUM8], command = COMMAND.SHIFT_WINDOW_WORKSPACE_8 },
        new() { keys = [VK.LMENU, VK.LSHIFT, VK.NUM9], command = COMMAND.SHIFT_WINDOW_WORKSPACE_9 },
        // jump to numbered workspace
        new() { keys = [VK.LMENU, VK.NUM1], command = COMMAND.FOCUS_WORKSPACE_1 },
        new() { keys = [VK.LMENU, VK.NUM2], command = COMMAND.FOCUS_WORKSPACE_2 },
        new() { keys = [VK.LMENU, VK.NUM3], command = COMMAND.FOCUS_WORKSPACE_3 },
        new() { keys = [VK.LMENU, VK.NUM4], command = COMMAND.FOCUS_WORKSPACE_4 },
        new() { keys = [VK.LMENU, VK.NUM5], command = COMMAND.FOCUS_WORKSPACE_5 },
        new() { keys = [VK.LMENU, VK.NUM6], command = COMMAND.FOCUS_WORKSPACE_6 },
        new() { keys = [VK.LMENU, VK.NUM7], command = COMMAND.FOCUS_WORKSPACE_7 },
        new() { keys = [VK.LMENU, VK.NUM8], command = COMMAND.FOCUS_WORKSPACE_8 },
        new() { keys = [VK.LMENU, VK.NUM9], command = COMMAND.FOCUS_WORKSPACE_9 },
        // move/resize windows using mouse without titlebars
        new() { keys = [VK.LCONTROL, VK.SPACE], command = COMMAND.WINDOW_MOVE_MODE_ON },
        new() { keys = [VK.LMENU, VK.SPACE], command = COMMAND.WINDOW_RESIZE_MODE_ON },
        // system
        new() { keys = [VK.LCONTROL, VK.LSHIFT, VK.R], command = COMMAND.RESTART },
        new() { keys = [VK.LCONTROL, VK.LSHIFT, VK.U], command = COMMAND.UPDATE },
    };

    public string ToJson()
    {
        JsonObject j = new()
        {
            ["loglevel"] = loglevel,
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
                rules
                    .Select(rule => new JsonObject()
                    {
                        ["type"] = rule.type,
                        ["method"] = rule.method,
                        ["identifierType"] = rule.identifierType,
                        ["identifier"] = rule.identifier,
                    })
                    .ToArray()
            ),
            ["keymaps"] = new JsonArray(
                keymaps
                    .Select(keymap => new JsonObject()
                    {
                        ["keys"] = new JsonArray(
                            keymap.keys.Select(key => (JsonNode)key.ToString()).ToArray()
                        ),
                        ["command"] = keymap.command.ToString(),
                        ["arguments"] = new JsonArray(
                            keymap.arguments.Select(arg => (JsonNode)arg).ToArray()
                        ),
                    })
                    .ToArray()
            ),
        };
        return j.ToString();
    }

    public static Config FromJson(string json)
    {
        JsonNode node = JsonNode.Parse(json);

        Config config = new();
        config.loglevel = node?[nameof(loglevel)]?.ToString() ?? config.loglevel;
        config.layout = node?[nameof(layout)]?.ToString() ?? config.layout;
        config.inner = Convert.ToInt32(node?[nameof(inner)]?.ToString() ?? $"{config.inner}");
        config.left = Convert.ToInt32(node?[nameof(left)]?.ToString() ?? $"{config.left}");
        config.top = Convert.ToInt32(node?[nameof(top)]?.ToString() ?? $"{config.top}");
        config.right = Convert.ToInt32(node?[nameof(right)]?.ToString() ?? $"{config.right}");
        config.bottom = Convert.ToInt32(node?[nameof(bottom)]?.ToString() ?? $"{config.bottom}");
        config.workspaces = Convert.ToInt32(
            node?[nameof(workspaces)]?.ToString() ?? $"{config.workspaces}"
        );
        config.workspaceAnimations = node?[nameof(workspaceAnimations)]?.ToString() switch
        {
            "true" => true,
            "false" => false,
            _ => false,
        };
        config.workspaceAnimationsDuration = Convert.ToInt32(
            node?[nameof(config.workspaceAnimationsDuration)]?.ToString()
                ?? $"{config.workspaceAnimationsDuration}"
        );
        config.workspaceAnimationsDirection =
            node?[nameof(workspaceAnimationsDirection)]?.ToString()
            ?? config.workspaceAnimationsDirection;
        config.floatingWindowSize =
            node?[nameof(floatingWindowSize)]?.ToString() ?? config.floatingWindowSize;
        config.serverPort = Convert.ToInt32(
            node?[nameof(serverPort)]?.ToString() ?? $"{config.serverPort}"
        );

        config.rules = new();
        JsonArray? _rules = node?[nameof(config.rules)]?.AsArray();
        _rules
            ?.ToList()
            .ForEach(_rule =>
            {
                WindowRule rule = new();

                rule.type = _rule?[nameof(rule.type)]?.ToString() ?? rule.type;
                rule.method = _rule?[nameof(rule.method)]?.ToString() ?? rule.method;
                rule.identifierType =
                    _rule?[nameof(rule.identifierType)]?.ToString() ?? rule.identifierType;
                rule.identifier = _rule?[nameof(rule.identifier)]?.ToString() ?? rule.identifier;

                config.rules.Add(rule);
            });

        //config.keymaps = new();
        JsonArray _keymaps = node?[nameof(config.keymaps)]?.AsArray() ?? [];
        foreach (var _keymap in _keymaps)
        {
            Keymap keymap = new();

            // keys
            JsonArray _keys = _keymap?[nameof(keymap.keys)]?.AsArray() ?? [];
            bool parsed = false;
            foreach (var _key in _keys)
            {
                parsed = Enum.TryParse<VK>(_key?.ToString(), true, out VK vkKey);
                if (!parsed)
                    break;
                keymap.keys.Add(vkKey);
            }
            if (!parsed)
                continue; // the previous break and this continue ensures that if there is even
            // one incorrect VK key added it will omit the entire keymap. We do the same if an incorrect
            // COMMAND is provided too (below).

            // command
            string? _command = _keymap?[nameof(keymap.command)]?.ToString();
            if (!Enum.TryParse<COMMAND>(_command, true, out keymap.command))
                continue;

            // arguments
            JsonArray? _arguments = _keymap?[nameof(keymap.arguments)]?.AsArray() ?? [];
            foreach (var _arg in _arguments)
            {
                keymap.arguments.Add(_arg?.ToString() ?? "");
            }

            // add to keymaps only if there already isn't one in the the default with the same keys
            // if there is replace it, only add after removing the duplicates
            var _listEqual = (List<VK> a, List<VK> b) =>
            {
                if (a.Count != b.Count)
                    return false;
                for (int i = 0; i < a.Count; i++)
                {
                    if (a[i] != b[i])
                        return false;
                }
                return true;
            };
            var _matches = config
                .keymaps.Where(_keymap => _listEqual(_keymap.keys, keymap.keys))
                .ToList();
            if (_matches.Count != 0)
                foreach (var _match in _matches)
                    config.keymaps.Remove(_match);
            config.keymaps.Add(keymap);
        }

        return config;
    }
}

public class WindowRule
{
    public string type = ""; // ignore, floating
    public string method = ""; // equals, contains
    public string identifierType = ""; // windowProcess, windowTitle, windowClass
    public string identifier = ""; // search string
}
