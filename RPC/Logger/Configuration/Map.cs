namespace RPC.Logger.Configuration
{
    public class Map : Mask
    {
        public string? Name { get; set; } = null;
        public bool OriginKey { get; set; } = false;

        public string? MapName(string key)
        {
            return Name ?? (OriginKey ? key : Key);
        }
    }
}
