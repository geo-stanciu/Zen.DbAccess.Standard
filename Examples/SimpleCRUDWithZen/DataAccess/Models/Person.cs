using DataAccess.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
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

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    private string? _line_as_json;

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonDbType]
    public string? line_as_json
    {
        get
        {
            if (_line_as_json == null)
            {
                _line_as_json = this.ToJson();
            }

            return _line_as_json;
        }

        set
        {
            _line_as_json = value;
        }
    }
}
