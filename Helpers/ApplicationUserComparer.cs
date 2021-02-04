using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using WebApi.Entities;

namespace WebApi.Helpers
{
    public class ApplicationUserComparer : IEqualityComparer<ApplicationUser>
    {
        public bool Equals(ApplicationUser x, ApplicationUser y)
        {
            return x.Id == y.Id;
        }

        public int GetHashCode([DisallowNull] ApplicationUser obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}