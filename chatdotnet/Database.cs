using System;
using System.IO;
using System.Data.SQLite;
using System.Collections.Generic;

namespace chatdotnet
{
    class Database
    {
        private string servername; // server's randomly generated name
        private SQLiteConnection conn;

        private const string createServers =
                "create table servers(" +
                "name text unique not null);";

        private const string createChats =
                "create table chats(" +
                "id integer primary key autoincrement," +
                "name text not null);";

        private const string createMessages =
                "create table messages(" +
                "id int not null," +
                "chat integer not null," +
                "servername text not null," +
                "type int not null," +
                "msg text not null," +
                "sender text not null," +
                "raw blob," +
                "foreign key(chat) references chats(id)," +
                "foreign key(servername) references servers(name));";

        internal Database()
        {
            string dbdir = Environment.ExpandEnvironmentVariables(@"%userprofile%\chatdotnet");
            string dbpath = dbdir + @"\chatdb";

            bool create = !File.Exists(dbpath);
            if (create)
            {
                Directory.CreateDirectory(dbdir);
                SQLiteConnection.CreateFile(dbpath);
            }

            conn = new SQLiteConnection("Data Source=" + dbpath + ";Version=3;");
            conn.Open();

            if (create)
            {
                CreateTables();
            }
        }

        internal void Close()
        {
            conn.Close();
        }

        // insert server name into the database if it does not already exist
        internal void SetServerName(string nm)
        {
            servername = nm;
            string query =
                "select * from servers where name = @name";

            // see if it exists already
            var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@name",nm);
            var reader = cmd.ExecuteReader();
            bool exists = reader.HasRows;
            reader.Dispose();
            cmd.Dispose();
            if (exists)
                return;

            // insert it 
            string insert =
                "insert into servers values (@servername);";
            cmd = new SQLiteCommand(insert, conn);
            cmd.Parameters.AddWithValue("@servername",servername);
            cmd.ExecuteNonQuery();
            cmd.Dispose();
        }

        // add chat to database, only if it doesn't already exist
        internal void AddChat(Chat chat)
        {
            bool exists = ChatExists(chat);
            if (!exists)
            {
                // add it
                string insert =
                    "insert into chats (name) values (@name);";

                var cmd = new SQLiteCommand(insert, conn);
                cmd.Parameters.AddWithValue("@name", chat.name);
                cmd.ExecuteNonQuery();
                cmd.Dispose();
            }
        }

        // get the most recent message for a given chat
        internal ulong GetLatest(Chat chat)
        {
            int max = 0;
            string query =
                "select max(id) from messages where chat = @chat and servername = @server;";

            var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@chat", chat.id);
            cmd.Parameters.AddWithValue("@server", servername);
            object val = cmd.ExecuteScalar();
            if (val != null && !val.ToString().Equals(""))
                max = Convert.ToInt32(val.ToString());

            cmd.Dispose();

            return (ulong)max;
        }

        // get all messages in a chat
        internal List<Message> GetMessages(Chat chat)
        {
            var list = new List<Message>();
            string query =
                "select * from messages where chat = @chat and servername = @server;";

            var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@chat", chat.id);
            cmd.Parameters.AddWithValue("@server", servername);
            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                /*
                // get bytes is bugged and doesn't work.
                long rawlen = reader.GetBytes(6, 0, null, 0, 0);
                Log.Write("rawlen is = " + rawlen);
                byte[] raw = null;
                if(rawlen > 0)
                {
                    raw = new byte[rawlen];
                    reader.GetBytes(6, 0, raw, 0, (int)rawlen);
                }
                */

                var message = new Message((MessageType)reader.GetInt32(3), (ulong)reader.GetInt32(0), reader.GetString(4), reader.GetString(5), null);
                list.Add(message);
            }

            reader.Dispose();
            cmd.Dispose();

            return list;
        }

        // add a message to the database
        internal void AddMessage(Chat chat, Message msg)
        {
            string insert = "insert into messages values(@id,@chat,@server,@type,@msg,@sender,@blob)";

            var cmd = new SQLiteCommand(insert, conn);
            cmd.Parameters.AddWithValue("@id", msg.id);
            cmd.Parameters.AddWithValue("@chat", chat.id);
            cmd.Parameters.AddWithValue("@server", servername);
            cmd.Parameters.AddWithValue("@type", msg.type);
            cmd.Parameters.AddWithValue("@msg", msg.text);
            cmd.Parameters.AddWithValue("@sender", msg.sender);
            cmd.Parameters.AddWithValue("@blob", msg.raw);
            cmd.ExecuteNonQuery();
            cmd.Dispose();
        }

        private void CreateTables()
        {
            var cmd = new SQLiteCommand(createChats + createServers + createMessages, conn);
            cmd.ExecuteNonQuery();
            cmd.Dispose();
        }

        private bool ChatExists(Chat chat)
        {
            string query = "select name from chats where name = @name;";

            var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@name", chat.name);
            var reader = cmd.ExecuteReader();
            bool exists = reader.HasRows;
            reader.Dispose();
            cmd.Dispose();

            return exists;
        }
    }
}
