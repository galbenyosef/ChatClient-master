using ChatAppCore;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ChatApp
{
    public partial class LoginWindow : Window
    {
        const bool REGVIEW = false;
        const bool LOGVIEW = true;
        const bool DEFAULTVIEW = LOGVIEW;
        const string requestString = "http://localhost:40433/chat/";
        const string timeFormat = "dd/MM/yyyy HH:mm:ss";

        Contact self;

        public string Username { get; set; }
        public string Fullname { get; set; }
        public LoginWindow()
        {
            InitializeComponent();
            DataContext = this;
            changeView(DEFAULTVIEW);
            setClickables();
            self = new Contact();
            tryConnectByProperties();
        }
        void tryConnectByProperties()
        {
            if (Properties.Settings.Default.userName != string.Empty)
            {
                Username = Properties.Settings.Default.userName;
                txt1_password.Password = Properties.Settings.Default.passWord;
                PerformClick(btn_login);
            }
        }

        void changeView(bool valview)
        {
            if (valview.Equals(REGVIEW))
            {
                txt1_fullname.Visibility = Visibility.Visible;
                btn_register.Visibility = Visibility.Visible;
                btn_login.Visibility = Visibility.Hidden;
                txt1_register.Visibility = Visibility.Hidden;
                txt1_login.Visibility = Visibility.Visible;
                lbl1_fullname.Visibility = Visibility.Visible;
                lbl1_username.Visibility = Visibility.Visible;
                lbl1_password.Visibility = Visibility.Visible;

            }
            else
            {
                txt1_fullname.Visibility = Visibility.Hidden;
                btn_register.Visibility = Visibility.Hidden;
                btn_login.Visibility = Visibility.Visible;
                txt1_register.Visibility = Visibility.Visible;
                txt1_login.Visibility = Visibility.Hidden;
                lbl1_fullname.Visibility = Visibility.Hidden;
                lbl1_username.Visibility = Visibility.Visible;
                lbl1_password.Visibility = Visibility.Visible;
            }
        }

        void PerformClick(Button btnObject)

        {
            btnObject.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, btnObject));
        }

        void setClickables()
        {
            txt1_register.PreviewMouseDown += setViewREG;
            txt1_login.PreviewMouseDown += setViewLOG;
        }

        void setViewREG(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            changeView(REGVIEW);
        }
        void setViewLOG(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            changeView(LOGVIEW);
        }

        void openChatDialog()
        {
            ChatWindow window;
            window = new ChatWindow(self);
            window.Show();
        }

        async void btn_register_Click(object sender, RoutedEventArgs e)
        {
            if (!validateDigitsChars(txt1_password.Password))
            {
                lbl1_passworderr.Content = "Password can contain only digits and letters";
                return;
            }
            if (!validateDigitsChars(Username))
            {
                lbl1_usernameerr.Content = "Username can contain only digits and letters";
                return;
            }
            if (!validateDigitsCharsWs(Fullname))
            {
                lbl1_fullnameerr.Content = "Full name can contain only digits or letters (atleast one) and whitespaces";
                return;
            }
            clearErrors();
            string parameters = "register";
            btn_register.IsEnabled = false;
            btn_register.Content = "Registering...";
            self.Username = Username;
            self.Password = txt1_password.Password;
            self.Name = Fullname;
            try
            {
                self.CreatedOn = await getTimeAsync();
                var serialized = JsonConvert.SerializeObject(self);
                var content = new StringContent(serialized, Encoding.UTF8, "application/json");
                using (var c = new HttpClient())
                {
                    var request = await c.PostAsync(requestString + parameters, content);
                    if (!request.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Registration failed!");
                        btn_register.IsEnabled = true;
                        btn_register.Content = "Register";
                        return;
                    }
                }
                MessageBox.Show("You are now registered!");
                PerformClick(btn_login);
            }
            catch
            {
                return;
            }
        }

        async void btn_login_Click(object sender, RoutedEventArgs e)
        {
            if (!validateDigitsChars(txt1_password.Password))
            {
                lbl1_passworderr.Content = "Password can contain only digits and letters";
                return;
            }
            if (!validateDigitsChars(Username))
            {
                lbl1_usernameerr.Content = "Username can contain only digits and letters";
                return;
            }
            clearErrors();
            string parameters = "login";
            btn_login.IsEnabled = false;
            btn_login.Content = "Loggin in...";
            self.Username = Username;
            self.Password = txt1_password.Password;
            var serialized = JsonConvert.SerializeObject(self);
            var content = new StringContent(serialized, Encoding.UTF8, "application/json");
            try
            {
                using (var c = new HttpClient())
                {
                    var request = await c.PostAsync(requestString + parameters, content);
                    if (!request.IsSuccessStatusCode)
                    {
                        btn_login.IsEnabled = true;
                        btn_login.Content = "Login";
                        MessageBox.Show("Login failed!");
                        return;
                    }
                    MessageBox.Show($"{Username}, you are now logged in!");
                    string respond = request.Content.ReadAsStringAsync().Result;
                    self = JsonConvert.DeserializeObject<Contact>(respond);
                    if (chk_remember.IsChecked ?? false)
                    {
                        Properties.Settings.Default.userName = Username;
                        Properties.Settings.Default.passWord = txt1_password.Password;
                        Properties.Settings.Default.Save();
                    }
                    openChatDialog();
                    Close();
                }
            }
            catch
            {
                return;
            }
        }

        bool validateDigitsChars(string text)
        {
            if (text.Length < 1)
                return false;
            foreach (char single in text)
            {
                if (!char.IsLetterOrDigit(single))
                    return false;
            }
            return true;
        }

        bool validateDigitsCharsWs(string text)
        {
            bool letterDetected = false;
            if (text.Length < 1)
                return false;
            foreach (char single in text)
            {
                if (char.IsLetterOrDigit(single))
                    letterDetected = true;
                if (!char.IsLetterOrDigit(single) && !char.IsWhiteSpace(single))
                    return false;
            }
            if (!letterDetected)
                return false;
            return true;
        }

        void clearErrors()
        {
            lbl1_usernameerr.Content = string.Empty;
            lbl1_passworderr.Content = string.Empty;
            lbl1_fullnameerr.Content = string.Empty;
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
    }
}