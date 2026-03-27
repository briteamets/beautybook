using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace App.Domain;

public class ListItem : BaseEntity
{
    [StringLength(128,MinimumLength = 1)]
    public string ItemDescription { get; set; } = default!;
 
    public LangStr Summary { get; set; } = default!;

    public bool IsDone { get; set; }
}