using System;
using Newtonsoft.Json;
using TrickCore;

namespace FirebaseWebGL.Scripts.Objects
{
    [Serializable, JsonObject]
    public class FirebaseError
    {
        public string code;
        public string message;
        public string name;
        public string stack;

        public override string ToString()
        {
            if (string.IsNullOrEmpty(message))
                return this.SerializeToJson(true, false);
            return message;
        }

        public static FirebaseError FromException(AggregateException taskException)
        {
            if (taskException != null)
            {
                var baseException = taskException.GetBaseException();
                return new FirebaseError()
                {
                    code = "",
                    message = baseException.Message,
                };
            }

            return new FirebaseError()
            {
                message = "Unknown error."
            };
        }

        public static FirebaseError FromException(Exception exception)
        {
            if (exception != null)
            {
                var baseException = exception.GetBaseException();
                return new FirebaseError()
                {
                    code = "",
                    message = baseException.Message,
                };
            }

            return new FirebaseError()
            {
                message = "Unknown error."
            };
        }
    }
}
