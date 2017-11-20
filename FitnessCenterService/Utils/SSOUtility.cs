using System;

namespace FitnessCenterService.Utils
{
    public class SSOUtility
    {
        public static int GenerateSSO()
        {
            return Guid.NewGuid().GetHashCode();
        }
    }
}