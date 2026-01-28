using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI;
using AbaAbilities.Core.UI;

namespace AbaAbilities.Common.UI;

public abstract class PagedUIState : UIState
{
    public event Action OnClose;

    protected ShadedBackground Background { get; private set; }
    protected UIElement ButtonContainer { get; private set; }
    protected List<PageButton> Buttons { get; } = new();

    private float _timeOpen;
    private bool _closing;

    protected virtual string BackgroundTexture => "AbaAbilities/Assets/Textures/UI/Background_Shaded_Center";
    protected virtual float BackgroundFadeDuration => 0.3f;
    protected virtual float ButtonDelay => 0.05f;
    protected virtual float ButtonAnimDuration => 0.3f;
    protected virtual float ButtonContainerWidth => 600f;
    protected virtual float ButtonContainerHeight => 60f;
    protected virtual float ButtonContainerVAlign => 0.65f;

    public bool IsFullyClosed { get; private set; } = true;

    public override void OnInitialize() {
        Background = new ShadedBackground(BackgroundTexture);
        Background.Width.Set(0, 1f);
        Background.Height.Set(0, 1f);
        Append(Background);

        ButtonContainer = new UIElement();
        ButtonContainer.Width.Set(ButtonContainerWidth, 0f);
        ButtonContainer.Height.Set(ButtonContainerHeight, 0f);
        ButtonContainer.HAlign = 0.5f;
        ButtonContainer.VAlign = ButtonContainerVAlign;
        Append(ButtonContainer);

        InitializeContent();
    }

    protected abstract void InitializeContent();

    protected void AddButton(PageButton button) {
        Buttons.Add(button);
        ButtonContainer.Append(button);
    }

    public override void OnActivate() {
        _timeOpen = 0f;
        _closing = false;
        IsFullyClosed = false;
        Background?.Reset();
        foreach (var btn in Buttons) {
            btn.Reset();
        }
        OnPageActivate();
    }

    protected virtual void OnPageActivate() { }

    public void RequestClose() {
        if (_closing)
            return;
        _closing = true;
        IsFullyClosed = true;
        OnClose?.Invoke();
    }

    public override void Update(GameTime gameTime) {
        base.Update(gameTime);
        Recalculate();
        if (_closing)
            return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _timeOpen += dt;

        float bgT = MathHelper.Clamp(_timeOpen / BackgroundFadeDuration, 0f, 1f);
        Background?.SetProgress(EaseOutQuad(bgT));

        float btnTime = _timeOpen - ButtonDelay;
        float btnT = btnTime > 0 ? MathHelper.Clamp(btnTime / ButtonAnimDuration, 0f, 1f) : 0f;
        float easedBtnT = EaseOutQuad(btnT);
        foreach (var btn in Buttons) {
            btn.SetEntryProgress(easedBtnT);
        }

        if (ShouldCloseOnKey() && Main.LocalPlayer.controlInv) {
            RequestClose();
            Main.LocalPlayer.releaseInventory = false;
        }

        OnPageUpdate(gameTime, dt);
    }

    protected virtual void OnPageUpdate(GameTime gameTime, float deltaTime) { }

    protected virtual bool ShouldCloseOnKey() => true;

    private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
}
