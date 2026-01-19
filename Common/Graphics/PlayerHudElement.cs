using Microsoft.Xna.Framework;

namespace AbaAbilities.Common.Graphics
{
    /// <summary>
    /// Represents a HUD element to be drawn on the player.
    /// </summary>
    public struct PlayerHudElement
    {
        /// <summary>
        /// The text to display. Supports Terraria chat tags (e.g. [i:123]).
        /// </summary>
        public string Text;

        /// <summary>
        /// The color of the text. Defaults to White if null.
        /// </summary>
        public Color TextColor;

        /// <summary>
        /// The progress of the bar (0.0 to 1.0). If null, no bar is drawn.
        /// </summary>
        public float? BarProgress;

        /// <summary>
        /// The color of the filled portion of the bar. Defaults to Green if null.
        /// </summary>
        public Color BarColor;

        /// <summary>
        /// The color of the unfilled portion of the bar. Defaults to dark gray.
        /// </summary>
        public Color UnfilledBarColor;

        /// <summary>
        /// The height/thickness of the bar in pixels. Defaults to 8f.
        /// </summary>
        public float BarThickness;

        /// <summary>
        /// The color of the border. Defaults to Black.
        /// </summary>
        public Color BorderColor;

        /// <summary>
        /// The thickness of the border. Defaults to 2f;
        /// </summary>
        public float BorderThickness;

        /// <summary>
        /// The scale of the text. Defaults to 1f.
        /// </summary>
        public float FontSize;

        /// <summary>
        /// If true, renders like a Terraria health bar using Hb1/Hb2 textures with green-yellow-red color transitions.
        /// </summary>
        public bool UseHealthBarStyle;

        /// <summary>
        /// The width of the bar in pixels. Defaults to 36f (Terraria health bar width). Only used for non-health-bar style.
        /// </summary>
        public float BarWidth;

        /// <summary>
        /// The gap in pixels between text and bar. Positive values push text up (away from bar). Defaults to 4f.
        /// </summary>
        public float TextGap;

        public PlayerHudElement(
            string text = null,
            Color? textColor = null,
            float? barProgress = null,
            Color? barColor = null,
            Color? unfilledBarColor = null,
            float barThickness = 8f,
            Color? borderColor = null,
            float borderThickness = 2f,
            float fontSize = 1f,
            bool useHealthBarStyle = false,
            float barWidth = 36f,
            float textGap = 4f)
        {
            Text = text;
            TextColor = textColor ?? Color.White;
            BarProgress = barProgress;
            BarColor = barColor ?? Color.Green;
            UnfilledBarColor = unfilledBarColor ?? new Color(40, 40, 40, 200);
            BarThickness = barThickness;
            BorderColor = borderColor ?? Color.Black;
            BorderThickness = borderThickness;
            FontSize = fontSize;
            UseHealthBarStyle = useHealthBarStyle;
            BarWidth = barWidth;
            TextGap = textGap;
        }
    }
}
