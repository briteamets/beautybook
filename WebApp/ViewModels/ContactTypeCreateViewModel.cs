using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels;

public class ContactTypeCreateViewModel
{
    [StringLength(128, MinimumLength = 1)]
    public string ContactTypeName { get; set; } = default!;
}