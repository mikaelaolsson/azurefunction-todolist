using Newtonsoft.Json.Converters;

namespace ToDoList.Models {
    public class CustomDateTimeConverter : IsoDateTimeConverter {
        public CustomDateTimeConverter() {
            base.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        }
    }
}
