using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FitnessCenterService.Utils
{
    public class JsonUtility
    {
        private const string DateFormat = "dd/MM/yyyy HH:mm";
        private static readonly IsoDateTimeConverter _dateTimeConverter = new IsoDateTimeConverter { DateTimeFormat = DateFormat };

        public static string ObjectToJson(object @object)
        {
            //var jsonSerializerSettings = new JsonSerializerSettings
            //{
            //    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            //    Converters = {_dateTimeConverter}
            //};
            return JsonConvert.SerializeObject(@object, _dateTimeConverter);
        }

        public static T JsonToObject<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, _dateTimeConverter);
        }
    }
}