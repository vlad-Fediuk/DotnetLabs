namespace ECommerce.Shared.Supabase;

public class SupabaseSettings
{
    public const string SectionName = "Supabase";

    public string Url { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public bool AutoConnectRealtime { get; set; } = true;
}
