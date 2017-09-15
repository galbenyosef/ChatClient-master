using System.Collections.Generic;

namespace ChatAppCore
{
    public class PendingItems
    {
        public ICollection<Message> PendingMessages;
        public ICollection<Contact> PendingContacts;

        public bool hasChanged, hasMessages, hasContacts;
    }
}
