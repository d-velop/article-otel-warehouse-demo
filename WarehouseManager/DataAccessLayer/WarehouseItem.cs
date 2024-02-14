using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WarehouseManager.DataAccessLayer;

public class WarehouseItem
{
    public Guid Id { get; set; }

    [Required]
    [Display(Name = "name")]
    public string Name{ get; set; }

    [Required]
    [Display(Name = "itemValue")]
    public int ItemValue { get; set; }
    
    [Required]
    [Display(Name = "itemQty")]
    public int ItemQty { get; set; }
}
