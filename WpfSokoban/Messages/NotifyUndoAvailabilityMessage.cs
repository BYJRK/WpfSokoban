using CommunityToolkit.Mvvm.Messaging.Messages;

namespace WpfSokoban.Messages
{
    public class NotifyUndoAvailabilityMessage : ValueChangedMessage<string>
    {
        public NotifyUndoAvailabilityMessage(string commandName) : base(commandName)
        {
        }
    }
}
