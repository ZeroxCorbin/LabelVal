using CommunityToolkit.Mvvm.Messaging.Messages;
using System;

namespace LabelVal.Messages;
public class SystemMessages
{
    public enum StatusMessageType
    {
        Info,
        Warning,
        Error,
        Control
    }

    public class StatusMessage : ValueChangedMessage<StatusMessageType>
    {
        public object Sender { get; private set; }
        private string message;
        public string Message => Exception != null ? Exception.Message : message;
        public Exception? Exception { get; private set; }

        public StatusMessage(object sender, StatusMessageType type, string message, Exception? exception = null) : base(type)
        {
            Sender = sender;
            this.message = message;
            Exception = exception;
        }
    }

    public class ErrorMessage : ValueChangedMessage<Exception>
    {
        public ErrorMessage(Exception exception) : base(exception)
        {

        }
    }
}
