using Terraria.ModLoader.Config;

namespace AbaAbilities.Common.Config
{
    public class ServerConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Label("Debug/Dev Mode")]
        public bool DevMode { get; set; } = false;
    }
}