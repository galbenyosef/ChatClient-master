using ChatAppCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace ChatApp
{
    public partial class ChatWindow : Window
    {
        const int IntervalSeconds = 10;
        const string timeFormat = "dd/MM/yyyy HH:mm:ss";
        const string requestString = "http://localhost:40433/chat/";
        CancellationTokenSource intervalCancelToken = new CancellationTokenSource();
        public static int self_id;
        public List<Message> MessagesStored;

        //Data bound properties
        public Contact Self { get; set; }
        public Contact Receiver { get; set; }
        public string Body { get; set; }
        public ObservableCollection<string> LastUpdated { get; set; }
        //Data binded collections
        public ObservableCollection<Contact> Contacts { get; set; }
        public ObservableCollection<Message> Messages { get; set; }
        public ChatWindow(Contact self)
        {
            InitializeComponent();
            DataContext = this;
            MessagesStored = new List<Message>();
            //Receiver = new Contact();
            Contacts = new ObservableCollection<Contact>();
            Messages = new ObservableCollection<Message>();
            LastUpdated = new ObservableCollection<string>();
            LastUpdated.Add(getTime());
            Self = self;
            self_id = self.ID;
            initializeContent();
        }
        async void initializeContent()
        {
            var contacts = await getContactsAsync();
            var messages = await getMessagesAsync();
            fillContacts(contacts);
            fillMessages(messages);
            attachLastMessages();
            setLastUpdate();
            startInterval(intervalCancelToken.Token);
        }

        async Task<List<Contact>> getContactsAsync()
        {
            string parameters = "contacts";
            try
            {
                using (HttpClient c = new HttpClient())
                {
                    var response = await c.GetAsync(requestString + parameters);
                    if (response.IsSuccessStatusCode)
                    {
                        string resultContent = response.Content.ReadAsStringAsync().Result;
                        return new List<Contact>(JsonConvert.DeserializeObject<IList<Contact>>(resultContent));
                    }
                    return new List<Contact>();
                }
            }
            catch
            {
                MessageBox.Show("Server offline.");
                return new List<Contact>();
            }
        }

        void fillContacts(List<Contact> contacts)
        {
            contacts.Remove(contacts.Single(cnt => cnt.ID == self_id));
            Contacts.Clear();
            contacts.ForEach(Contacts.Add);
        }

        async Task<List<Message>> getMessagesAsync()
        {
            string parameters = $"messages/{ Self.ID }";
            try
            {
                using (HttpClient c = new HttpClient())
                {
                    var response = await c.GetAsync(requestString + parameters);
                    if (response.IsSuccessStatusCode)
                    {
                        string resultContent = response.Content.ReadAsStringAsync().Result;
                        return new List<Message>(JsonConvert.DeserializeObject<IList<Message>>(resultContent));
                    }
                }
                return new List<Message>();
            }
            catch
            {
                MessageBox.Show("Server offline.");
                return new List<Message>();
            }
        }

        void fillMessages(List<Message> messages)
        {
            Messages.Clear();
            messages.ForEach(MessagesStored.Add);
        }
        void filterMessages()
        {
            if (Receiver == null)
                return;
            Messages.Clear();
            foreach (var message in MessagesStored
                .Where(msg => msg.Sender.ID == Receiver.ID || msg.Receiver.ID == Receiver.ID)
                .OrderBy(msg => msg.Date, new DateComparer()))
                Messages.Add(message);
            if (lb2_messages.Items.Count > 0)
                lb2_messages.ScrollIntoView(lb2_messages.Items[lb2_messages.Items.Count - 1]);

        }
        void attachLastMessages()
        {
            if (MessagesStored.Count == 0)
                return;
            foreach (var contact in Contacts)
            {
                var conversation = MessagesStored.Where(msg => msg.Receiver.ID == contact.ID || msg.Sender.ID == contact.ID);
                if (conversation.Count() > 0)
                {
                    contact.LastMessage = conversation.OrderByDescending(message => message.Date)
                    .ElementAt(0);
                }
            }
            refreshObservableCollection(Contacts);
        }
        async Task<string> getTimeAsync()
        {
            string parameters = "timeasync";
            try
            {
                using (var c = new HttpClient())
                {
                    var timeResult = await c.GetAsync(requestString + parameters);
                    string resultContent = timeResult.Content.ReadAsStringAsync().Result;
                    return JsonConvert.DeserializeObject<DateTime>(resultContent).ToString(timeFormat);
                }
            }
            catch
            {
                return "Server offline.";
            }
        }
        string getTime()
        {
            string parameters = "time";
            try
            {
                using (var c = new HttpClient())
                {
                    var timeResult = c.GetAsync(requestString + parameters).Result;
                    string resultContent = timeResult.Content.ReadAsStringAsync().Result;
                    return JsonConvert.DeserializeObject<DateTime>(resultContent).ToString(timeFormat);
                }
            }
            catch
            {
                return "Server offline.";
            }
        }
        string getLastUpdate()
        {
            return LastUpdated[0];
        }
        async void setLastUpdate()
        {
            var theTime = await getTimeAsync();
            LastUpdated.Clear();
            LastUpdated.Add(theTime);
            refreshObservableCollection(LastUpdated);
        }
        void refreshObservableCollection<T>(ObservableCollection<T> obsCol)
        {
            CollectionViewSource.GetDefaultView(obsCol).Refresh();
        }
        void startInterval(CancellationToken token)
        {
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(IntervalSeconds);
            timer.Tick += new EventHandler(async (object s, EventArgs a) =>
            {
                await fetchPendingItemsAsync();
            });
            timer.Start();

        }
        async Task<bool> fetchPendingItemsAsync()
        {
            string timeFormatAPI = "ddMMyyyyHHmmss";
            string time = Convert.ToDateTime(getLastUpdate()).ToString(timeFormatAPI);
            string parameters = $"{Self.ID}" + "/since/" + $"{time}";
            try
            {
                using (HttpClient c = new HttpClient())
                {
                    var response = await c.GetAsync(requestString + parameters);
                    if (response.IsSuccessStatusCode)
                    {
                        string resultContent = response.Content.ReadAsStringAsync().Result;
                        PendingItems newItems = (JsonConvert.DeserializeObject<PendingItems>(resultContent));
                        if (newItems.hasChanged)
                        {
                            if (newItems.hasMessages)
                            {
                                newItems.PendingMessages.ToList().ForEach(MessagesStored.Add);
                                filterMessages();
                            }
                            if (newItems.hasContacts)
                            {
                                newItems.PendingContacts.ToList().ForEach(Contacts.Add);
                            }
                            attachLastMessages();
                        }
                    }
                }
                setLastUpdate();
                return true;
            }
            catch
            {
                return false;
            }
        }
        private void lb2_contacts_Click(object sender, SelectionChangedEventArgs e)
        {
            filterMessages();
        }

        async void btn2_send_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txt2_body.Text) || Receiver == null)
            {
                return;
            }
            string parameters = "newmessage";
            try
            {
                using (var c = new HttpClient())
                {
                    var Composed = new Message();
                    Composed.Sender = Self;
                    Composed.Receiver = Receiver;
                    Composed.Body = new string(Body.ToCharArray());
                    Composed.Date = await getTimeAsync();
                    var serialized = JsonConvert.SerializeObject(Composed, Formatting.Indented, new JsonSerializerSettings()
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    });
                    var content = new StringContent(serialized, Encoding.UTF8, "application/json");
                    var response = await c.PostAsync(requestString + parameters, content);
                    if (response.IsSuccessStatusCode)
                    {
                        MessagesStored.Add(Composed);
                        filterMessages();
                        attachLastMessages();
                        refreshObservableCollection(Contacts);
                        txt2_body.Clear();
                    }
                }
            }
            catch
            {
                return;
            }
        }
        void btn2_logout_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.userName = string.Empty;
            Properties.Settings.Default.passWord = string.Empty;
            Properties.Settings.Default.Save();
            LoginWindow window = new LoginWindow();
            window.Show();
            Close();
        }
    }

    public class MessagingDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement elemnt = container as FrameworkElement;
            Message msg = item as Message;
            if (msg.Sender.ID != ChatWindow.self_id)
            {
                return elemnt.FindResource("MessageReceived") as DataTemplate;
            }
            else
            {
                return elemnt.FindResource("MessageSent") as DataTemplate;
            }
        }
    }

    public class DateComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            return Convert.ToDateTime(x).CompareTo(Convert.ToDateTime(y));
        }
    }

}