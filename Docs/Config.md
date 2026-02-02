```json
{
    "layout": "dwindle", // dwindle, stack, master
    "left": 10, // left margin
    "top": 40, // top margin
    "right": 10, // right margin
    "bottom": 10, // bottom margin
    "inner": 10, // gaps between windows
    "workspaces": 9, // number of workspaces
    "workspaceAnimations": true, // true, false
    "workspaceAnimationsDuration": 500, // milliseconds
    "workspaceAnimationsDirection": "horizontal", // horizontal, vertical
    "floatingWindowSize": "800x400",
    "serverPort": 6969, // websocket port
    "rules": 
    [
        {
            "type": "ignore", // ignore, floating
            "method": "equals", // equals, contains
            "identifierType": "windowProcess", // windowProcess, windowTitle, windowClass
            "identifier": "Flow.Launcher" // search string
        }
    ],
    "keymaps": 
    [
        {
              "keys": // keys as VK codes
              [
                  "LCONTROL",
                  "LSHIFT",
                  "L"
              ],
            "command": "FOCUS_NEXT_WORKSPACE", // Commands as in the COMMAND enum
            "arguments": []
        }
    ]
}
```
