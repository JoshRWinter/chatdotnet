using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using System.Windows.Forms;
using System.Drawing;

namespace chatdotnet
{
    public class Front : Form
    {
        private ChatClient client;

        private TextBox messageView;
        private TextBox editor;
        private Button send;

        public Front()
        {
            Text = "ayy";
            Size = new Size(450, 595);
            CenterToScreen();
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            List<Message> stored = null;
            messageView = new TextBox() { ReadOnly = true, Width = 410, Height = 400, Left = 10, Top = 10, Multiline = true };
            editor = new TextBox() {  Width = 410, Height = 100, Left = 10, Top = 420, Multiline = true };
            send = new Button() { Width = 410, Text = "Send", Left = 10, Top = 530 };
            send.Click += (sender, e) =>
            {
                client.Message(editor.Text, (success, msg) =>
                {
                    if(!success)
                        Invoke(new Action<string>(MsgReceipt), msg);
                });
                editor.Text = "";
            };

            Tuple<string, string> t = new ServerPrompt().Exec();
            if(t != null)
            {
                client = new ChatClient();
                bool worked = Connect(t.Item1, t.Item2);
                if(worked != false)
                {
                    List<Chat> chats = ListChats();
                    if (chats != null)
                    {

                        Chat selected = new SelectChat(chats, AddChat).Exec();
                        if (selected != null)
                        {
                            stored = Subscribe(selected);
                        }
                        else
                        {
                            Console.WriteLine("you didn't select anything");
                        }
                    }
                }

                if(stored != null)
                {
                    foreach (Message msg in stored)
                        ShowMessage(msg);
                }

                Controls.Add(messageView);
                Controls.Add(editor);
                Controls.Add(send);
            }
        }

        public new void Close()
        {
            client?.Shutdown();
            base.Close();
        }

        public bool AddChat(string name, string description)
        {
            bool worked = false;
            bool done = false;
            Mutex mutex = new Mutex();

            NewChatCallback nc = (success) =>
            {
                worked = success;
                mutex.WaitOne();
                done = true;
                mutex.ReleaseMutex();
            };

            client.NewChat(name, description, nc);

            bool cached = false;
            do
            {
                mutex.WaitOne();
                cached = done;
                mutex.ReleaseMutex();
            } while (!cached);

            return worked;
        }

        public static void Main(string[] args)
        {
            Front front = new Front();
            Application.Run(front);
            front.Close();
        }

        private bool Connect(string server, string name)
        {
            bool worked = false;
            bool done = false;
            Mutex wait = new Mutex();

            ConnectCallback callback = (success) =>
            {
                worked = success;
                wait.WaitOne();
                done = true;
                wait.ReleaseMutex();
            };
            client.Connect(server, name, callback);

            bool cached = false;
            do
            {
                wait.WaitOne();
                cached = done;
                wait.ReleaseMutex();
            } while (!cached);

            return worked;
        }

        private List<Chat> ListChats()
        {
            List<Chat> chats = null;
            bool done = false;
            Mutex mutex = new Mutex();

            ListChatCallback callback = (chatList) =>
            {
                mutex.WaitOne();
                chats = chatList;
                done = true;
                mutex.ReleaseMutex();
            };

            client.ListChats(callback);

            bool cached = false;
            do
            {
                mutex.WaitOne();
                cached = done;
                mutex.ReleaseMutex();
            } while (!cached);

            return chats;
        }

        private List<Message> Subscribe(Chat chat)
        {
            bool done = false;
            bool worked = false;
            Mutex mutex = new Mutex();
            List<Message> stored = null;

            SubscribeCallback sc = (success, msgs) =>
            {
                worked = success;
                stored = msgs;
                mutex.WaitOne();
                done = true;
                mutex.ReleaseMutex();
            };

            client.Subscribe(chat, sc, NewMessage);

            bool cached = false;
            do
            {
                mutex.WaitOne();
                cached = done;
                mutex.ReleaseMutex();
            } while (!cached);

            return stored;
        }

        public void ShowMessage(Message msg)
        {
            messageView.Text += "> " + msg.sender + ": " + msg.text + "\r\n";
        }

        public void MsgReceipt(string msg)
        {
            MessageBox.Show(msg);
        }

        public void NewMessage(Message msg)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<Message>(ShowMessage), msg);
            }
        }
    }

    // ask client for server
    public class ServerPrompt : Form
    {
        TextBox nametext;
        TextBox servertext;
        Button connect;

        // ask for server url
        internal ServerPrompt()
        {
            Width = 375;
            Height = 150;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Text = "Connect to a server";
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;

            Label serverLabel = new Label() { Text = "Server Address", Left = 5, Top = 20 };
            Label nameLabel = new Label() { Text = "Your Name/Nickname", Left = 5, Top = 50 };
            servertext = new TextBox { Left = 120, Top = 20, Width = 200 };
            nametext = new TextBox { Left = 120, Top = 50, Width = 200 };
            connect = new Button { Left = 160, Top = 80, Text = "Connect", DialogResult = DialogResult.OK };
            connect.Click += (sender, e) =>
            {
                Close();
            };

            // temporary
            nametext.Text = "josh winter";
            servertext.Text = "192.168.1.50";

            Controls.Add(serverLabel);
            Controls.Add(nameLabel);
            Controls.Add(servertext);
            Controls.Add(nametext);
            Controls.Add(connect);
            AcceptButton = connect;
        }
        
        internal Tuple<string, string> Exec()
        {
            return ShowDialog() == DialogResult.OK ? new Tuple<string, string>(servertext.Text, nametext.Text) : null;
        }
    }

    public delegate bool NewChat(string name, string description);

    class SelectChat : Form
    {
        Button subscribe;
        Button newchat;
        ListBox list;
        List<Chat> chats;

        // ask for server url
        internal SelectChat(List<Chat> chts, NewChat del)
        {
            Width = 300;
            Height = 455;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Text = "Select a Session";
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;

            chats = chts;
            list = new ListBox() { Width = 275, Height = 350, Left = 5, Top = 5 };
            foreach(Chat chat in chats)
            {
                list.Items.Add(chat);
            }
            if(chats.Count > 0)
                list.SelectedIndex = 0;
            subscribe = new Button { Left = 10, Top = 355, Width = 260, Text = "Subscribe", DialogResult = DialogResult.OK };
            subscribe.Click += (sender, e) =>
            {
                Close();
            };
            newchat = new Button() { Text = "New Session", Left = 10, Width = 260, Top = 385 };
            newchat.Click += (sender, e) =>
            {
                Tuple<string, string> result = new MakeChat().Exec();
                if(result != null)
                    del(result.Item1, result.Item2);
            };

            Controls.Add(list);
            Controls.Add(subscribe);
            Controls.Add(newchat);
            AcceptButton = subscribe;
        }
        
        internal Chat Exec()
        {
            return ShowDialog() == DialogResult.OK ? (Chat)list.SelectedItem : null;
        }
    }

    class MakeChat : Form
    {
        private TextBox chatname;
        private TextBox description;
        private Button make;

        internal MakeChat()
        {
            Text = "Make a new Session";
            Width = 400;
            Height = 170;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            Label labelChatName = new Label() { Text = "Chat Name", Left = 10, Top = 10 };
            Label labelDescription = new Label() { Text = "Description", Left = 10, Top = 40 };

            chatname = new TextBox() { Width = 260, Left = 110, Top = 10 };
            description = new TextBox() { Multiline = true, Width = 260, Height = 50, Left = 110, Top = 40 };

            make = new Button() { Text = "Create", Width = 75, Left = 180, Top = 100, DialogResult = DialogResult.OK };
            make.Click += (sender, e) =>
            {
                Close();
            };

            Controls.Add(labelChatName);
            Controls.Add(labelDescription);
            Controls.Add(chatname);
            Controls.Add(description);
            Controls.Add(make);
        }

        internal Tuple<string, string> Exec()
        {
            return ShowDialog() == DialogResult.OK ? new Tuple<string, string>(chatname.Text, description.Text) : null;
        }
    }
}
