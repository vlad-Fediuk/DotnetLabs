using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Supabase;

namespace ECommerce.Shared.Supabase;

public static class SupabaseServiceExtensions
{
    public static IServiceCollection AddSupabaseClient(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(SupabaseSettings.SectionName);
        var settings = section.Get<SupabaseSettings>() ?? new SupabaseSettings();

        var url = !string.IsNullOrWhiteSpace(settings.Url)
            ? settings.Url
            : configuration["SUPABASE_URL"] ?? "https://qkvkyiflslckmmnwqkvq.supabase.co";

        var key = !string.IsNullOrWhiteSpace(settings.PublishableKey)
            ? settings.PublishableKey
            : configuration["SUPABASE_KEY"] ?? "sb_publishable_F3j9pWgMGvdzR8wNJnqS9g_n_XF_NlC";

        services.Configure<SupabaseSettings>(options =>
        {
            options.Url = url;
            options.PublishableKey = key;
            options.AutoConnectRealtime = settings.AutoConnectRealtime;
        });

        services.AddSingleton(sp =>
        {
            var options = new global::Supabase.SupabaseOptions
            {
                AutoConnectRealtime = settings.AutoConnectRealtime
            };

            var client = new Client(url, key, options);
            return client;
        });

        return services;
    }
}
