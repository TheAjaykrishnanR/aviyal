class Window
{
	nint hWnd;
	string title;
	string exe;
	RECT rect;

	void Hide() { };
	void Show(RECT pos) { };
	void Focus() { };
	void Move(RECT target) { };
}

class Workspace
{
	List<Window> windows;
	Window focusedWindow;

	// intended to work on itself
	void Focus();
	// intended to work on other objects
	void FocusWindow(Window wnd) { };
}

class WindowManager
{
	List<Window> windows; // all windows
	List<Workspaces> workspaces;
	Workspace focusedWorkspace;

	// intended to work on other objects
	void FocusWorkspace(Workspace target) { };
	void MoveWindow(Workspace target) { };

	// events
	event windowCreated;
	event windowDragged;
	event windowClosed;

	// constant
	int margin; // top, bottom , right, left
}

/* 
 * 1. Window manager collects all windows
 * 2. Creates workspaces and assigns all to workspace 1 by default
 * 3. listen to window event (creation, deletion, minimization, movement) and act accordingly
 *
 * */


