using System;
using System.Configuration;
using System.Drawing;
using System.Dynamic;

namespace FitnessCenterService.Utils
{
    public class Settings
    {
        #region Constants

        private const string SessionExpirationTimeoutKey = "SessionExpirationTimeout";
        private const int DefaultSessionExpirationTimeout = 30; //in minutes
        #endregion

        #region Fields
        private static int _sessionExpirationTimeout;
        #endregion

        #region LifeCycle
        static Settings()
        {
            if (!int.TryParse(ConfigurationManager.AppSettings[SessionExpirationTimeoutKey],
                    out _sessionExpirationTimeout))
            {
                _sessionExpirationTimeout = DefaultSessionExpirationTimeout;
            }
        }
        #endregion

        #region Properties

        public static int SessionExpirationTimeout
        {
            get
            {
                return _sessionExpirationTimeout;
            }
        }

        #endregion
    }
}