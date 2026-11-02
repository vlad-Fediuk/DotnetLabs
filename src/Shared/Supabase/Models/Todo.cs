using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ECommerce.Shared.Supabase.Models;

[Table("todos")]
public class Todo : BaseModel
{
    [PrimaryKey("id")]
    public long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;
}
