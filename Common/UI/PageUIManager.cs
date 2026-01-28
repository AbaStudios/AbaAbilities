using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;

namespace AbaAbilities.Common.UI;

public class PageUIManager
{
    public event Action OnFullClose;

    public UserInterface Interface { get; private set; }
    public bool IsOpen { get; private set; }

    private readonly Stack<PagedUIState> _pageStack = new();
    private GameTime _lastGameTime;

    public PageUIManager() {
        Interface = new UserInterface();
    }

    public PagedUIState CurrentPage => _pageStack.Count > 0 ? _pageStack.Peek() : null;

    public void Open(PagedUIState initialPage) {
        _pageStack.Clear();
        _pageStack.Push(initialPage);
        Interface.SetState(initialPage);
        IsOpen = true;
        initialPage.OnClose += HandlePageClose;
    }

    public void PushPage(PagedUIState page) {
        if (CurrentPage != null) {
            CurrentPage.OnClose -= HandlePageClose;
        }
        _pageStack.Push(page);
        Interface.SetState(page);
        page.OnClose += HandlePageClose;
    }

    public void PopPage() {
        if (_pageStack.Count <= 1) {
            Close();
            return;
        }

        var current = _pageStack.Pop();
        current.OnClose -= HandlePageClose;

        var previous = _pageStack.Peek();
        Interface.SetState(previous);
        previous.OnClose += HandlePageClose;
    }

    public void Close() {
        foreach (var page in _pageStack) {
            page.OnClose -= HandlePageClose;
        }
        _pageStack.Clear();
        Interface.SetState(null);
        IsOpen = false;
        OnFullClose?.Invoke();
    }

    private void HandlePageClose() {
        if (_pageStack.Count <= 1) {
            Close();
        }
        else {
            PopPage();
        }
    }

    public void Update(GameTime gameTime) {
        _lastGameTime = gameTime;
        if (!IsOpen)
            return;
        Interface?.Update(gameTime);
    }

    public void Draw(SpriteBatch spriteBatch) {
        if (!IsOpen || _lastGameTime == null)
            return;
        Interface?.Draw(spriteBatch, _lastGameTime);
    }
}
