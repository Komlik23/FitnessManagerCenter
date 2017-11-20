using System;

namespace FitnessCenterService
{
    public class RolesAttribute : Attribute
    {
        public RolesAttribute(string[] roles)
        {
            Roles = roles;
        }

        public string[] Roles { get; }
    }
}