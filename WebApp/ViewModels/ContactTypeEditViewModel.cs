using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace WebApp.ViewModels;

public class ContactTypeEditViewModel : BaseEntity
{
    [StringLength(128, MinimumLength = 1)]
    public string ContactTypeName { get; set; } = default!;
}