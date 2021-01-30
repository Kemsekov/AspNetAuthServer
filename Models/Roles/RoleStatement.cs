using System.Collections.Generic;

namespace WebApi.Models.Roles
{
    public class RoleStatement
    {
        public List<string> AndRoles{get;set;} = new List<string>();
        public RoleStatement NextOrRole{get;set;}
    }
}