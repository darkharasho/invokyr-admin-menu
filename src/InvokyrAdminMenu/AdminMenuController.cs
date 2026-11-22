using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using User;
using Cursor = UnityEngine.Cursor;

namespace InvokyrAdminMenu
{
    /// <summary>
    /// Owns the menu: reads the hotkey, opens and closes the window, takes the game's input while it's open, and draws
    /// it. While open the game's input actions are off (<c>InputsManager.Disable</c>), the cursor is free and the uGUI
    /// event system is off so clicks don't reach the game's own buttons underneath.
    /// </summary>
    public sealed class AdminMenuController : MonoBehaviour
    {
        public AdminMenuController(IntPtr ptr) : base(ptr) { }

        private const float ReferenceHeight = 1080f;
        private const float WindowWidth = 900f;
        private const float WindowHeight = 700f;

        private bool _open;
        private InputsManager _inputs;
        private EventSystem _eventSystem;
        private bool _disabledEvents;
        private CursorLockMode _priorLock;
        private bool _priorVisible;
        private bool _loggedError;

        private string _status = "";
        private string _toast;
        private float _toastUntil;
        private float _scroll;
        private float _contentHeight;
        /// <summary>Taken on Layout and drawn on Repaint, so a player leaving between the two can't change what's drawn.</summary>
        private List<PlayerRow> _rows = new List<PlayerRow>();

        private void Update()
        {
            try
            {
                if (_open)
                    MenuInput.Poll(Screen.height / ReferenceHeight);
                Tick();
            }
            catch (Exception e)
            {
                LogOnce("update", e);
                if (_open)
                    Close(lost: true);
            }
        }

        private void LateUpdate()
        {
            try
            {
                SpendingLock.LateTick();
            }
            catch (Exception e)
            {
                LogOnce("refund", e);
            }
            if (_open)
                KeepCursorFree();
        }

        private void OnDestroy()
        {
            if (_open)
                Close(lost: true);
        }

        private void Tick()
        {
            if (_open)
            {
                if (!Players.Hosting)
                {
                    Close(lost: true);
                    return;
                }
                if (Plugin.CloseOnEscape.Value && Hotkey.EscapePressed())
                {
                    Close(lost: false);
                    return;
                }
                KeepCursorFree();
            }

            if (!Hotkey.WasPressed(Plugin.ToggleShortcut))
                return;
            if (_open)
                Close(lost: false);
            else if (!Players.Hosting)
                ShowToast("Admin menu: host only");
            else
                Open();
        }

        private void Open()
        {
            _priorLock = Cursor.lockState;
            _priorVisible = Cursor.visible;
            _inputs = InputsManager.Instance;
            _inputs?.Disable();
            _eventSystem = EventSystem.current;
            if (_eventSystem != null && _eventSystem.enabled)
            {
                _eventSystem.enabled = false;
                _disabledEvents = true;
            }
            MenuInput.Reset();
            _open = true;
            _scroll = 0f;
            _status = "";
            KeepCursorFree();
        }

        /// <param name="lost">Hosting ended: hand input back but leave the cursor to the game.</param>
        private void Close(bool lost)
        {
            if (!_open)
                return;
            _open = false;
            MenuInput.Reset();
            try
            {
                _inputs?.Enable();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Couldn't turn the game's input back on: {e.Message}");
            }
            _inputs = null;
            if (_disabledEvents && _eventSystem != null)
                _eventSystem.enabled = true;
            _disabledEvents = false;
            _eventSystem = null;
            if (!lost)
            {
                Cursor.lockState = _priorLock;
                Cursor.visible = _priorVisible;
            }
        }

        private static void KeepCursorFree()
        {
            if (Cursor.lockState != CursorLockMode.None)
                Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible)
                Cursor.visible = true;
        }

        private void OnGUI()
        {
            if (!_open && (_toast == null || Time.unscaledTime >= _toastUntil))
                return;
            try
            {
                var type = Event.current.type;
                if (_open && type == EventType.Layout)
                    Snapshot();
                // Only Repaint draws and handles input, so a click is acted on exactly once per frame.
                if (type != EventType.Repaint)
                    return;

                Theme.Ensure();
                GUI.depth = -1000;
                var scale = Screen.height / ReferenceHeight;
                GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
                var width = Screen.width / scale;
                if (_open)
                {
                    MenuInput.BeginPass();
                    DrawMenu(width);
                    MenuInput.EndPass();
                }
                else
                {
                    DrawToast(width);
                }
            }
            catch (Exception e)
            {
                LogOnce("draw", e);
                if (_open)
                    Close(lost: true);
            }
            finally
            {
                GUI.matrix = Matrix4x4.identity;
            }
        }

        private void Snapshot()
        {
            _rows = Players.Rows();
            var changed = false;
            foreach (var row in _rows)
                changed |= Plugin.Rules.Remember(row.Id, row.IsHost);
            if (changed)
                Plugin.SaveRules();
        }

        private void DrawToast(float width)
        {
            var w = Theme.TextWidth(_toast, Theme.Toast) + 40f;
            var h = Theme.Toast.fontSize * 1.35f + 24f;
            GUI.Label(new Rect((width - w) / 2f, ReferenceHeight * 0.12f, w, h), _toast, Theme.Toast);
        }

        private void DrawMenu(float width)
        {
            GUI.DrawTexture(new Rect(0f, 0f, width, ReferenceHeight), Theme.Dim);
            var window = new Rect((width - WindowWidth) / 2f, (ReferenceHeight - WindowHeight) / 2f, WindowWidth, WindowHeight);
            GUI.Label(window, "", Theme.WindowStyle);

            var x = window.x + 24f;
            var w = window.width - 48f;
            var top = window.y + 20f;
            const float headerHeight = 54f + 30f;
            const float statusHeight = 30f;
            var view = new Rect(x, top + headerHeight, w, window.yMax - 20f - statusHeight - (top + headerHeight));

            // Content first; the covers hide any row that is half outside the viewport.
            DrawScrolled(view);
            var inner = new Rect(window.x + 3f, window.y + 3f, window.width - 6f, window.height - 6f);
            GUI.Label(new Rect(inner.x, inner.y, inner.width, view.y - inner.y), "", Theme.Cover);
            GUI.Label(new Rect(inner.x, view.yMax, inner.width, inner.yMax - view.yMax), "", Theme.Cover);

            GUI.Label(new Rect(x, top, w - 130f, 48f), "ADMIN MENU", Theme.Title);
            if (Header(new Rect(x + w - 110f, top + 4f, 110f, 40f), "Close", Theme.Button))
                Close(lost: false);
            var blocked = _rows.FindAll(r => !Plugin.Rules.IsAllowed(r.Id)).Count;
            GUI.Label(new Rect(x, top + 54f, w, 26f), $"{_rows.Count} players · {blocked} can't spend", Theme.MutedText);

            GUI.Label(new Rect(x, window.yMax - 20f - statusHeight + 4f, w, statusHeight), string.IsNullOrEmpty(_status) ? " " : _status, Theme.MutedText);
        }

        private static bool Header(Rect rect, string text, Btn style)
        {
            var hot = rect.Contains(MenuInput.Pos);
            GUI.Label(rect, text, hot ? style.Hover : style.Normal);
            if (!hot || !MenuInput.Click)
                return false;
            MenuInput.Click = false;
            return true;
        }

        private void DrawScrolled(Rect view)
        {
            const float bar = 6f;
            var maxScroll = Mathf.Max(0f, _contentHeight - view.height);
            if (MenuInput.Wheel != 0f && view.Contains(MenuInput.Pos))
                _scroll += MenuInput.Wheel * 60f;
            _scroll = Mathf.Clamp(_scroll, 0f, maxScroll);

            var pane = new Pane(view, view.width - bar - 14f, _scroll);
            DrawPlayers(pane);
            _contentHeight = pane.Y + _scroll;

            if (maxScroll > 0f)
            {
                GUI.Label(new Rect(view.xMax - bar, view.y, bar, view.height), "", Theme.Track);
                var thumbHeight = Mathf.Max(30f, view.height * view.height / _contentHeight);
                var thumbY = view.y + (view.height - thumbHeight) * (_scroll / maxScroll);
                GUI.Label(new Rect(view.xMax - bar, thumbY, bar, thumbHeight), "", Theme.Fill);
            }
        }

        private void DrawPlayers(Pane pane)
        {
            pane.Heading("SPENDING");
            foreach (var row in _rows)
            {
                var line = pane.Card();
                var allowed = Plugin.Rules.IsAllowed(row.Id);
                if (allowed)
                {
                    if (line.ButtonRight("Block spending", Theme.DangerButton, 190f))
                        SetAllowed(row, false);
                }
                else if (line.ButtonRight("Allow spending", Theme.Button, 190f))
                {
                    SetAllowed(row, true);
                }
                line.LabelRight(allowed ? "can spend" : "blocked", allowed ? Theme.MutedText : Theme.ErrorText, 110f);
                line.Label(row.Name + (row.IsHost ? "  (you)" : ""), Theme.Body);
            }

            var all = pane.Row(40f);
            if (all.Button("Allow everyone", Theme.Button))
                SetEveryone(true);
            if (all.Button("Block everyone else", Theme.DangerButton))
                SetEveryone(false);

            pane.Heading("NEW PLAYERS");
            var newLine = pane.Card();
            var blockNew = Plugin.BlockNewPlayers.Value;
            if (newLine.ButtonRight(blockNew ? "Turn off" : "Turn on", Theme.Button, 130f))
            {
                Plugin.BlockNewPlayers.Value = !blockNew;
                Plugin.SaveRules();
                _status = !blockNew ? "Players joining for the first time start blocked" : "Players joining for the first time can spend";
            }
            newLine.Label(blockNew ? "First-time players start blocked" : "First-time players can spend", Theme.Body);

            pane.Space(8f);
            pane.Text(
                "Blocked players can't buy from the shop or pay for room skins; the team's money stays where it is and they get a message. " +
                "Only the host needs this mod. Choices are saved by Steam id.",
                Theme.MutedText);
        }

        private void SetAllowed(PlayerRow row, bool allowed)
        {
            Plugin.Rules.Set(row.Id, allowed);
            Plugin.SaveRules();
            _status = allowed ? $"{row.Name} can spend again" : $"{row.Name} can't spend money now";
        }

        private void SetEveryone(bool allowed)
        {
            foreach (var row in _rows)
            {
                if (!row.IsHost)
                    Plugin.Rules.Set(row.Id, allowed);
            }
            Plugin.SaveRules();
            _status = allowed ? "Everyone can spend" : "Only you can spend";
        }

        private void ShowToast(string message)
        {
            _toast = message;
            _toastUntil = Time.unscaledTime + 3f;
        }

        private void LogOnce(string where, Exception e)
        {
            if (_loggedError)
                return;
            _loggedError = true;
            Plugin.Log.LogError($"Admin menu {where} failed. {e}");
        }
    }
}
