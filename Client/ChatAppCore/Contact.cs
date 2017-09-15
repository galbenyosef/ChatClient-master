namespace ChatAppCore
{
    public class Contact
    {
        public int ID { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public string CreatedOn { get; set; }
        //For client side purposes only
        public Message LastMessage { get; set; }
    }
}
