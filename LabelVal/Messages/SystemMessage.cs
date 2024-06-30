using CommunityToolkit.Mvvm.Messaging.Messages;
using Org.BouncyCastle.Tls;
using System;
using System.Runtime.CompilerServices;

namespace LabelVal.Messages;
public class SystemMessages
{
    public enum StatusMessageType
    {
        Debug,
        Info,
        Warning,
        Error,
        Control
    }

    public class StatusMessage : ValueChangedMessage<StatusMessageType>
    {
        private readonly string message;
        public string Message => Exception != null ? Exception.Message : message;

        public Exception Exception { get; private set; } = null;
        public bool HasException => Exception != null;

        public StatusMessage(string message, StatusMessageType type) : base(type) => this.message = message;
        public StatusMessage(string message) : base(StatusMessageType.Info) => this.message = message;
        public StatusMessage(Exception exception) : base(StatusMessageType.Error) => Exception = exception;
        public StatusMessage(string message, Exception exception) : base(StatusMessageType.Error) { Exception = exception; this.message = message; }
    }

    public class ControlMessage : ValueChangedMessage<object>
    {
        public object Sender { get; private set; }
        public string Message { get; private set; }
        public ControlMessage(object sender, string message) : base(StatusMessageType.Control)
        {
            this.Sender = sender; Message = message;
        }
    }
}
