using System.ComponentModel.DataAnnotations;

namespace SqliteProvider.Tables;

public class UserProductBindingTable
{
    [Key]
    public Guid Id { get; set; }
    public long UserChatId { get; set; }
    public Guid ProductId { get; set; }
    public Guid TeamId { get; set; }
}