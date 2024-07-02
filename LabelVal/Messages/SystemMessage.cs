using CommunityToolkit.Mvvm.Messaging.Messages;
using System;

namespace LabelVal.Messages;
public class SystemMessages
{
    public enum StatusMessageType
    {
        Debug,
        Info,
        Warning,
        Error,
    }

    public class StatusMessage : ValueChangedMessage<StatusMessageType>
    {
        public DateTime TimeStamp { get; } = DateTime.Now;

        //public bool Acknowledged { get; set; } = false;
        //public bool Acknowledge() => Acknowledged = true;

        private readonly string message;
        public string Message => Exception != null ? Exception.Message : message;

        public Exception Exception { get; private set; } = null;
        public bool HasException => Exception != null;

        public StatusMessage(string message, StatusMessageType type) : base(type) => this.message = message;
        public StatusMessage(string message) : base(StatusMessageType.Info) => this.message = message;
        public StatusMessage(Exception exception) : base(StatusMessageType.Error) => Exception = exception;
        public StatusMessage(string message, Exception exception) : base(StatusMessageType.Error) { Exception = exception; this.message = message; }
    }

    public class ControlMessage : ValueChangedMessage<string>
    {
        public object Sender { get; private set; }
        public ControlMessage(object sender, string message) : base(message) => this.Sender = sender;
    }
}
