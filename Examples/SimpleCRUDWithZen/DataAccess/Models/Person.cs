using DataAccess.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zen.DbAccess.Standard.Attributes;
using Zen.DbAccess.Standard.Models;

namespace DataAccess.Models;

public class Person : ResponseModel
{
    [PrimaryKey]
    public int Id { get; set; }
    public string? FirstName { get; set; }
    public string LastName { get; set; } = null!;
    public DateTime? BirthDate { get; set; }
    public PersonTypes? Type { get; set; }
    public byte[]? Image { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
