using System;
using System.Runtime.Serialization;
using NpgsqlTypes;

namespace epmgr.Data
{
    public enum DectDisplayModus
    {
        [PgName("NUMBER")]
        [EnumMember(Value = "NUMBER")]
        Num,
        [PgName("NUMBER_AND_NAME")]
        [EnumMember(Value = "NUMBER_NAME")]
        NumName,
        [PgName("NAME")]
        [EnumMember(Value = "NAME")]
        Name,
    }
}