using Data.Base.Exceptions;
using Data.Base.Models;

namespace Data.Base.Extensions
{
    public static class ExceptionExtensions
    {
        public static string LengthCheck(this string? value, int maxLength)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (value.Length > maxLength)
                throw new LengthValidationException(maxLength);
            return value;
        }

        public static void AddHistory(this DatastoreItem? item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            var history = new DatastoreItemHistory
            {
                State = item.State,
                Result = item.Result,
                Updated = item.Updated
            };
            item.History ??= new List<DatastoreItemHistory>();
            item.History.Add(history);
            item.Updated = DateTime.UtcNow;
        }
    }
}
