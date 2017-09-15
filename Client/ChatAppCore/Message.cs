namespace ChatAppCore
{
    public class Message
    {
        public int ID { get; set; }
        public Contact Sender { get; set; }
        public Contact Receiver { get; set; }
        public string Date { get; set; }
        public string Body { get; set; }
    }
}
