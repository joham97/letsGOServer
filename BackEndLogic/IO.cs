using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackEndLogic {
    public class IO {
        private static IO _Database;
        public static IO Database {
            get {
                if (_Database == null)
                    new IO();
                return _Database;
            }
        }

        private SQLiteConnection connection;
        private Dictionary<string, Session> sessions;

        public IO() {
            _Database = this;

            this.sessions = new Dictionary<string, Session>();
            this.InitDatabase(Config.CREATE_NEW_INITIAL_DATABASE);
        }

        #region out of session
        public string Registrieren(string username, string password, string mail, string phone) {
            string resp = "";

            this.log($"Try to regist user {username}");
            SQLiteCommand command = new SQLiteCommand(this.connection);
            command.CommandText = $"SELECT count(id) FROM account WHERE account.name='{username}'";
            SQLiteDataReader reader = command.ExecuteReader();
            bool exists = false;
            while (reader.Read()) {
                exists = reader[0].ToString() != "0";
            }
            reader.Close();
            reader.Dispose();
            command.Dispose();

            if (exists) {
                resp = new Response() { success = false, message = "Accountname existiert bereits" }.ToString();
                this.log($"Send answer: {resp}");
            } else {
                try {
                    //testdata
                    Random rnd = new Random();
                    int atp = rnd.Next(100, 1000);
                    int wp = rnd.Next(1, 100);
                    //testdata end
                    command = new SQLiteCommand(this.connection);
                    command.CommandText = $"INSERT INTO account (name, alltimepoints, weeklypoints, mail, lastactivity, phone, allowonlyfriends, password) VALUES('{username}', {atp}, {wp}, '{mail}', '{DateTime.Now}', '{phone}', 0, '{password}')";
                    command.ExecuteNonQuery();
                    resp = new Response() { success = true }.ToString();
                    this.log($"Send answer: {resp}");
                } catch (Exception e) {
                    resp = new Response() { success = false, message = "Bitte an Administrator wenden. Es ist ein fehler beim erstellen des Accounts aufgetreten." }.ToString();
                    this.log($"Send answer: {resp}");
                }
            }

            return resp;
        }

        public string Login(string username, string password) {
            string resp = "";

            this.log($"Try to login user {username}");
            SQLiteCommand command = new SQLiteCommand(this.connection);
            command.CommandText = $"SELECT count(id), id FROM account WHERE account.name='{username}' and account.password='{password}'";
            SQLiteDataReader reader = command.ExecuteReader();
            bool success = false;
            string userid = "";
            while (reader.Read()) {
                success = reader[0].ToString() != "0";
                userid = reader[1].ToString();
            }
            reader.Close();
            reader.Dispose();
            command.Dispose();

            if (!success) {
                resp = new Response() { success = false, message = "Accountname oder Passwort falsch." }.ToString();
                this.log($"Send answer: {resp}");
            } else {
                try {
                    string key = this.CreateSessionKey();
                    this.sessions.Add(key, new Session(Convert.ToInt32(userid), DateTime.Now.Ticks));
                    command = new SQLiteCommand(this.connection);
                    command.CommandText = $"UPDATE account SET lastactivity='{DateTime.Now}' WHERE id={userid}";
                    command.ExecuteNonQuery();
                    resp = new Response() { success = true, data = new Dictionary<string, object>() { { "SessionKey", key } } }.ToString();
                    this.log($"Send answer: {resp}");
                } catch (Exception e) {
                    resp = new Response() { success = false, message = "Bitte an Administrator wenden. Es ist ein fehler beim einloggen in den Account aufgetreten." }.ToString();
                    this.log($"Send answer: {resp}");
                }
            }

            return resp;
        }

        public string ForgetPassword(string username, string mail) {
            string resp = "";

            //check if it is a account with this combi
            //send mail to account with new password

            return resp;
        }
        #endregion

        #region with session key        
        public string Ranks(string type, string sessionkey) {
            string resp = "";

            this.log($"Try to get ranks from sessionkey {sessionkey}");
            if (this.IsValidSessionkey(sessionkey)) {
                bool valid = true;
                string select = "";
                switch (type) {
                    case "weekly":
                        select = $"SELECT id, name, alltimepoints, weeklypoints FROM account order by weeklypoints desc LIMIT 100;";
                        break;
                    case "alltime":
                        select = $"SELECT id, name, alltimepoints, weeklypoints FROM account order by alltimepoints desc LIMIT 100;";
                        break;
                    case "friends":
                        StringBuilder sb = null;
                        var friends = this.getFriends(sessionkey);
                        if (friends.Count == 0) {
                            sb = new StringBuilder("SELECT id, name, alltimepoints, weeklypoints FROM account order by alltimepoints desc LIMIT 0;");
                        } else {
                            sb = new StringBuilder("SELECT id, name, alltimepoints, weeklypoints FROM account");
                            sb.Append($" WHERE id={friends[0]}");
                            for (int i = 1; i < friends.Count - 1; i++) {
                                sb.Append($" OR id={friends[i]}");
                            }
                            sb.Append($" OR id={this.sessions[sessionkey].userid}");
                            sb.Append(" order by alltimepoints desc LIMIT 100;");
                        }
                        select = sb.ToString();
                        break;
                    default:
                        resp = new Response() { success = false, message = "Invalid type. Valid types are: weekly/alltime/friends" }.ToString();
                        this.log($"Send answer: {resp}");
                        valid = false;
                        break;
                }
                if (valid) {
                    try {
                        List<SingleUser> data = new List<SingleUser>();
                        SQLiteCommand command = new SQLiteCommand(this.connection);
                        command.CommandText = select;
                        SQLiteDataReader reader = command.ExecuteReader();
                        int platz = 1;
                        while (reader.Read()) {
                            data.Add(new SingleUser(reader[0].ToString(), reader[1].ToString(), reader[2].ToString(), reader[3].ToString(), platz.ToString()));
                            platz++;
                        }
                        reader.Close();
                        reader.Dispose();
                        command.Dispose();

                        this.KeepAlive(sessionkey);
                        resp = new Response() { success = true, data = new Dictionary<string, object>() { { "data", data } } }.ToString();
                        this.log($"Send answer: {resp}");
                    } catch (Exception e) {
                        resp = new Response() { success = false, message = "Bitte an Administrator wenden. Es ist ein fehler beim laden der Ranks aufgetreten." }.ToString();
                        this.log($"Send answer: {resp}");
                    }
                }
            } else {
                resp = new Response() { success = false, message = "Session abgelaufen." }.ToString();
                this.log($"Send answer: {resp}");
            }

            return resp;
        }

        public string LoadHomeScreen(string sessionkey) {
            string resp = "";

            //check if user exists in DB

            this.log($"Try to load HomeScreen from sessionkey {sessionkey}");
            if (this.IsValidSessionkey(sessionkey)) {
                try {
                    //clean DB

                    Response answer = new Response() { success = true };

                    SQLiteCommand command = new SQLiteCommand(this.connection);
                    command.CommandText = $"SELECT alltimepoints, weeklypoints FROM account WHERE account.id='{this.sessions[sessionkey].userid}'";
                    SQLiteDataReader reader = command.ExecuteReader();
                    string atp = "";
                    string wp = "";
                    while (reader.Read()) {
                        atp = reader[0].ToString();
                        wp = reader[1].ToString();
                    }
                    reader.Close();
                    reader.Dispose();
                    command.Dispose();
                    answer.data.Add("alltimepoints", atp);
                    answer.data.Add("weeklypoints", wp);

                    answer.data.Add("RunningYourTurn", new List<Dictionary<string, object>>());
                    answer.data.Add("RunningOtherPlayerTurn", new List<Dictionary<string, object>>());
                    answer.data.Add("Requested", new List<Dictionary<string, object>>());
                    answer.data.Add("GotInvited", new List<Dictionary<string, object>>());
                    answer.data.Add("History", new List<Dictionary<string, object>>());
                    Dictionary<string, object> temp;
                    command = new SQLiteCommand(this.connection);
                    command.CommandText = $"SELECT game.id, game.status, game.zugcount, game.playtime, game.lastturn, game.player1turn, game.fieldsize, account.name, game.newmsg FROM game inner join account on account.id=game.player2 WHERE game.player1={this.sessions[sessionkey].userid} order by game.lastturn desc;";
                    reader = command.ExecuteReader();
                    while (reader.Read()) {
                        if (reader[1].ToString() == "3") {
                            if (reader[5].ToString() == "1") {
                                temp = new Dictionary<string, object>();
                                temp.Add("id", reader[0].ToString());
                                temp.Add("zugcount", reader[2].ToString());
                                temp.Add("playtime", reader[3].ToString());
                                temp.Add("lastturn", reader[4].ToString());
                                temp.Add("fieldsize", reader[6].ToString());
                                temp.Add("otherplayername", reader[7].ToString());
                                temp.Add("newmsg", reader[8].ToString());
                                (answer.data["RunningYourTurn"] as List<Dictionary<string, object>>).Add(temp);
                            } else {
                                temp = new Dictionary<string, object>();
                                temp.Add("id", reader[0].ToString());
                                temp.Add("zugcount", reader[2].ToString());
                                temp.Add("playtime", reader[3].ToString());
                                temp.Add("lastturn", reader[4].ToString());
                                temp.Add("fieldsize", reader[6].ToString());
                                temp.Add("otherplayername", reader[7].ToString());
                                temp.Add("newmsg", reader[8].ToString());
                                (answer.data["RunningOtherPlayerTurn"] as List<Dictionary<string, object>>).Add(temp);
                            }
                        } else if (reader[1].ToString() == "1") {
                            temp = new Dictionary<string, object>();
                            temp.Add("id", reader[0].ToString());
                            temp.Add("playtime", reader[3].ToString());
                            temp.Add("lastturn", reader[4].ToString());
                            temp.Add("fieldsize", reader[6].ToString());
                            temp.Add("otherplayername", reader[7].ToString());
                            (answer.data["GotInvited"] as List<Dictionary<string, object>>).Add(temp);
                        } else {
                            //only5!!
                            temp = new Dictionary<string, object>();
                            temp.Add("zugcount", reader[2].ToString());
                            temp.Add("playtime", reader[3].ToString());
                            temp.Add("gamefinisheddatetime", reader[4].ToString());
                            temp.Add("fieldsize", reader[6].ToString());
                            temp.Add("otherplayername", reader[7].ToString());
                            temp.Add("newmsg", reader[8].ToString());
                            switch (reader[1].ToString()) {
                                case "2":
                                    temp.Add("end", "declined");
                                    temp.Add("youwon", "0");
                                    break;
                                case "4":
                                    temp.Add("end", "win");
                                    temp.Add("youwon", "1");
                                    break;
                                case "5":
                                    temp.Add("end", "loose");
                                    temp.Add("youwon", "0");
                                    break;
                                case "6":
                                    temp.Add("end", "giveup");
                                    temp.Add("youwon", "1");
                                    break;
                                case "7":
                                    temp.Add("end", "giveup");
                                    temp.Add("youwon", "0");
                                    break;
                                case "8":
                                    temp.Add("end", "overtime");
                                    temp.Add("youwon", "1");
                                    break;
                                case "9":
                                    temp.Add("end", "overtime");
                                    temp.Add("youwon", "0");
                                    break;
                                case "10":
                                    temp.Add("end", "accepttimeout");
                                    temp.Add("youwon", "0");
                                    break;
                                default:
                                    break;
                            }
                            (answer.data["History"] as List<Dictionary<string, object>>).Add(temp);
                        }
                    }
                    reader.Close();
                    reader.Dispose();
                    command.Dispose();
                    command = new SQLiteCommand(this.connection);
                    command.CommandText = $"SELECT game.id, game.status, game.zugcount, game.playtime, game.lastturn, game.player1turn, game.fieldsize, account.name, game.newmsg FROM game inner join account on account.id=game.player1 WHERE game.player2={this.sessions[sessionkey].userid} order by game.lastturn desc;";
                    reader = command.ExecuteReader();
                    while (reader.Read()) {
                        if (reader[1].ToString() == "3") {
                            if (reader[5].ToString() == "0") {
                                temp = new Dictionary<string, object>();
                                temp.Add("id", reader[0].ToString());
                                temp.Add("zugcount", reader[2].ToString());
                                temp.Add("playtime", reader[3].ToString());
                                temp.Add("lastturn", reader[4].ToString());
                                temp.Add("fieldsize", reader[6].ToString());
                                temp.Add("otherplayername", reader[7].ToString());
                                temp.Add("newmsg", reader[8].ToString());
                                (answer.data["RunningYourTurn"] as List<Dictionary<string, object>>).Add(temp);
                            } else {
                                temp = new Dictionary<string, object>();
                                temp.Add("id", reader[0].ToString());
                                temp.Add("zugcount", reader[2].ToString());
                                temp.Add("playtime", reader[3].ToString());
                                temp.Add("lastturn", reader[4].ToString());
                                temp.Add("fieldsize", reader[6].ToString());
                                temp.Add("otherplayername", reader[7].ToString());
                                temp.Add("newmsg", reader[8].ToString());
                                (answer.data["RunningOtherPlayerTurn"] as List<Dictionary<string, object>>).Add(temp);
                            }
                        } else if (reader[1].ToString() == "1") {
                            temp = new Dictionary<string, object>();
                            temp.Add("id", reader[0].ToString());
                            temp.Add("playtime", reader[3].ToString());
                            temp.Add("lastturn", reader[4].ToString());
                            temp.Add("fieldsize", reader[6].ToString());
                            temp.Add("otherplayername", reader[7].ToString());
                            (answer.data["Requested"] as List<Dictionary<string, object>>).Add(temp);
                        } else {
                            //only anothe 5!!
                            temp = new Dictionary<string, object>();
                            temp.Add("zugcount", reader[2].ToString());
                            temp.Add("playtime", reader[3].ToString());
                            temp.Add("gamefinisheddatetime", reader[4].ToString());
                            temp.Add("fieldsize", reader[6].ToString());
                            temp.Add("otherplayername", reader[7].ToString());
                            temp.Add("newmsg", reader[8].ToString());
                            switch (reader[1].ToString()) {
                                case "2":
                                    temp.Add("end", "declined");
                                    temp.Add("youwon", "0");
                                    break;
                                case "4":
                                    temp.Add("end", "loose");
                                    temp.Add("youwon", "0");
                                    break;
                                case "5":
                                    temp.Add("end", "win");
                                    temp.Add("youwon", "1");
                                    break;
                                case "6":
                                    temp.Add("end", "giveup");
                                    temp.Add("youwon", "0");
                                    break;
                                case "7":
                                    temp.Add("end", "giveup");
                                    temp.Add("youwon", "1");
                                    break;
                                case "8":
                                    temp.Add("end", "overtime");
                                    temp.Add("youwon", "0");
                                    break;
                                case "9":
                                    temp.Add("end", "overtime");
                                    temp.Add("youwon", "1");
                                    break;
                                case "10":
                                    temp.Add("end", "accepttimeout");
                                    temp.Add("youwon", "0");
                                    break;
                                default:
                                    break;
                            }
                            (answer.data["History"] as List<Dictionary<string, object>>).Add(temp);
                        }
                    }
                    reader.Close();
                    reader.Dispose();
                    command.Dispose();

                    this.KeepAlive(sessionkey);
                    resp = answer.ToString();
                    this.log($"Send answer: {resp}");
                } catch (Exception e) {
                    resp = new Response() { success = false, message = "Bitte an Administrator wenden. Es ist ein fehler beim Laden des Homescreens aufgetreten." }.ToString();
                    this.log($"Send answer: {resp}");
                }
            } else {
                resp = new Response() { success = false, message = "Session abgelaufen." }.ToString();
                this.log($"Send answer: {resp}");
            }

            return resp;
        }

        public string AddFriend(string sessionkey, string userid) {
            string resp = "";

            //check if user exists in DB

            this.log($"Try to add friend {userid} from sessionkey {sessionkey}");
            if (this.IsValidSessionkey(sessionkey)) {
                try {
                    var command = new SQLiteCommand(this.connection);
                    command.CommandText = $"INSERT INTO account_account (idf, ids) VALUES({this.sessions[sessionkey].userid}, {userid})";
                    command.ExecuteNonQuery();

                    this.KeepAlive(sessionkey);
                    resp = new Response() { success = true }.ToString();
                    this.log($"Send answer: {resp}");
                } catch (Exception e) {
                    resp = new Response() { success = false, message = "Bitte an Administrator wenden. Es ist ein fehler beim Freund hinzufügen aufgetreten." }.ToString();
                    this.log($"Send answer: {resp}");
                }
            } else {
                resp = new Response() { success = false, message = "Session abgelaufen." }.ToString();
                this.log($"Send answer: {resp}");
            }

            return resp;
        }
        
        public string RemoveFriend(string sessionkey, string userid) {
            string resp = "";

            //check if accountAccunt cnnection is existing in DB

            this.log($"Try to remove friend {userid} from sessionkey {sessionkey}");
            if (this.IsValidSessionkey(sessionkey)) {
                try {
                    var command = new SQLiteCommand(this.connection);
                    command.CommandText = $"DELETE FROM account_account WHERE idf={this.sessions[sessionkey].userid} AND ids={userid};";
                    command.ExecuteNonQuery();

                    this.KeepAlive(sessionkey);
                    resp = new Response() { success = true }.ToString();
                    this.log($"Send answer: {resp}");
                } catch (Exception e) {
                    resp = new Response() { success = false, message = "Bitte an Administrator wenden. Es ist ein fehler beim Freund löschen aufgetreten." }.ToString();
                    this.log($"Send answer: {resp}");
                }
            } else {
                resp = new Response() { success = false, message = "Session abgelaufen." }.ToString();
                this.log($"Send answer: {resp}");
            }

            return resp;
        }

        public string Friends(string sessionkey) {
            string resp = "";

            this.log($"Try to get friends from sessionkey {sessionkey}");
            if (this.IsValidSessionkey(sessionkey)) {
                try {
                    StringBuilder sb = null;
                    var friends = this.getFriends(sessionkey);
                    if (friends.Count == 0) {
                        sb = new StringBuilder("SELECT id, name, alltimepoints, weeklypoints FROM account order by alltimepoints desc LIMIT 0;");
                    } else {
                        sb = new StringBuilder("SELECT id, name, alltimepoints, weeklypoints FROM account");
                        sb.Append($" WHERE id={friends[0]}");
                        for (int i = 1; i < friends.Count - 1; i++) {
                            sb.Append($" OR id={friends[i]}");
                        }
                        sb.Append(" order by name asc LIMIT 100;");
                    }

                    List<SingleUser> data = new List<SingleUser>();
                    SQLiteCommand command = new SQLiteCommand(this.connection);
                    command.CommandText = sb.ToString();
                    SQLiteDataReader reader = command.ExecuteReader();
                    int platz = 1;
                    while (reader.Read()) {
                        data.Add(new SingleUser(reader[0].ToString(), reader[1].ToString(), reader[2].ToString(), reader[3].ToString(), platz.ToString()));
                        platz++;
                    }
                    reader.Close();
                    reader.Dispose();
                    command.Dispose();

                    this.KeepAlive(sessionkey);
                    resp = new Response() { success = true, data = new Dictionary<string, object>() { { "data", data } } }.ToString();
                    this.log($"Send answer: {resp}");
                } catch (Exception e) {
                    resp = new Response() { success = false, message = "Bitte an Administrator wenden. Es ist ein fehler beim Freunde abrufen aufgetreten." }.ToString();
                    this.log($"Send answer: {resp}");
                }
            } else {
                resp = new Response() { success = false, message = "Session abgelaufen." }.ToString();
                this.log($"Send answer: {resp}");
            }

            return resp;
        }

        /// <summary>
        /// deprecated may not needed
        /// </summary>
        /// <param name="sessionkey"></param>
        /// <returns></returns>
        public string KeepAlive(string sessionkey) {
            string resp = "";

            this.log($"Try to keep alive sessionkey {sessionkey}");
            if (this.IsValidSessionkey(sessionkey)) {
                try {
                    this.sessions[sessionkey].lastactivity = DateTime.Now.Ticks;

                    resp = new Response() { success = true }.ToString();
                    this.log($"Send answer: {resp}");
                } catch (Exception e) {
                    resp = new Response() { success = false, message = "Bitte an Administrator wenden. Es ist ein fehler beim session keep alive aufgetreten." }.ToString();
                    this.log($"Send answer: {resp}");
                }
            } else {
                resp = new Response() { success = false, message = "Session abgelaufen." }.ToString();
                this.log($"Send answer: {resp}");
            }

            return resp;
        }

        public string RequestGame(string sessionkey, string userid, string time, string size) {
            string resp = "";

            //check if user exists in DB
            //check if time and size is accessable
            //check if there is already a game pened between this two users

            this.log($"Try to request a game with {userid} from sessionkey {sessionkey}");
            if (this.IsValidSessionkey(sessionkey)) {
                try {
                    SQLiteCommand command = new SQLiteCommand(this.connection);
                    command.CommandText = $"SELECT allowonlyfriends FROM account WHERE id={userid};";
                    SQLiteDataReader reader = command.ExecuteReader();
                    bool allowsonlyfriends = false;
                    while (reader.Read()) {
                        allowsonlyfriends = reader[0].ToString() == "1";
                    }
                    reader.Close();
                    reader.Dispose();
                    command.Dispose();

                    bool canSend = true;
                    if (allowsonlyfriends) {
                        command = new SQLiteCommand(this.connection);
                        command.CommandText = $"SELECT ids FROM account_account WHERE idf={userid};";
                        reader = command.ExecuteReader();
                        bool isFriend = false;
                        while (reader.Read()) {
                            if (reader[0].ToString() == this.sessions[sessionkey].userid.ToString())
                                isFriend = true;
                        }
                        reader.Close();
                        reader.Dispose();
                        command.Dispose();

                        canSend = isFriend;
                    }

                    if (canSend) {
                        command = new SQLiteCommand(this.connection);
                        command.CommandText = $"INSERT INTO game (player1, player2, player1turn, newmsg, zugcount, playtime, lastturn, status, fieldsize) VALUES({userid}, {this.sessions[sessionkey].userid}, 1, 0, 0, {time}, '{DateTime.Now.Ticks}', 1, {size});";
                        command.ExecuteNonQuery();

                        resp = new Response() { success = true }.ToString();
                    } else {
                        resp = new Response() { success = false, message = "Der User möchte nur von Freunden denen er folgt herausgefordert werden." }.ToString();
                    }

                    this.KeepAlive(sessionkey);
                    this.log($"Send answer: {resp}");
                } catch (Exception e) {
                    resp = new Response() { success = false, message = "Bitte an Administrator wenden. Es ist ein fehler beim Spiel anfragen aufgetreten." }.ToString();
                    this.log($"Send answer: {resp}");
                }
            } else {
                resp = new Response() { success = false, message = "Session abgelaufen." }.ToString();
                this.log($"Send answer: {resp}");
            }

            return resp;
        }

        //missing
        public string RejectGame(string sessionkey, string id) {
            string resp = "";
            
            //check if game id exists

            this.log($"Try to reject a game {id} from sessionkey {sessionkey}");
            if (this.IsValidSessionkey(sessionkey)) {
                try {
                    var command = new SQLiteCommand(this.connection);
                    command.CommandText = $"delete from game where id={id};";
                    command.ExecuteNonQuery();

                    this.KeepAlive(sessionkey);
                    resp = new Response() { success = true }.ToString();
                    this.log($"Send answer: {resp}");
                } catch (Exception e) {
                    resp = new Response() { success = false, message = "Bitte an Administrator wenden. Es ist ein fehler beim Spiel zurückziehen aufgetreten." }.ToString();
                    this.log($"Send answer: {resp}");
                }
            } else {
                resp = new Response() { success = false, message = "Session abgelaufen." }.ToString();
                this.log($"Send answer: {resp}");
            }

            return resp;
        }

        public string DeclineGame(string sessionkey, string id) {
            string resp = "";

            //check if game id exists and if i am pemitted to do this

            this.log($"Try to decline a game {id} from sessionkey {sessionkey}");
            if (this.IsValidSessionkey(sessionkey)) {
                try {
                    var command = new SQLiteCommand(this.connection);
                    command.CommandText = $"update game set status=2 where id={id};";
                    command.ExecuteNonQuery();

                    this.KeepAlive(sessionkey);
                    resp = new Response() { success = true }.ToString();
                    this.log($"Send answer: {resp}");
                } catch (Exception e) {
                    resp = new Response() { success = false, message = "Bitte an Administrator wenden. Es ist ein fehler beim Spiel ablehnen aufgetreten." }.ToString();
                    this.log($"Send answer: {resp}");
                }
            } else {
                resp = new Response() { success = false, message = "Session abgelaufen." }.ToString();
                this.log($"Send answer: {resp}");
            }

            return resp;
        }

        public string AcceptGame(string sessionkey, string id) {
            string resp = "";

            //check if game id exists and if i am pemitted to do this

            this.log($"Try to accept a game {id} from sessionkey {sessionkey}");
            if (this.IsValidSessionkey(sessionkey)) {
                try {
                    var command = new SQLiteCommand(this.connection);
                    command.CommandText = $"update game set status=3 where id={id};";
                    command.ExecuteNonQuery();
                    
                    command = new SQLiteCommand(this.connection);
                    command.CommandText = $"select fieldsize from game where id={id};";
                    SQLiteDataReader reader = command.ExecuteReader();
                    int size = 0;
                    while (reader.Read()) {
                        size = Convert.ToInt32(reader[0].ToString());
                    }
                    reader.Close();
                    reader.Dispose();
                    command.Dispose();

                    for (int i = 0; i < size; i++) {
                        for (int j = 0; j < size; j++) {
                            command = new SQLiteCommand(this.connection);
                            command.CommandText = $"INSERT INTO singlefield (game, koordx, koordy, status) VALUES({id}, {i}, {j}, 0);";
                            command.ExecuteNonQuery();
                        }
                    }

                    this.KeepAlive(sessionkey);
                    resp = new Response() { success = true }.ToString();
                    this.log($"Send answer: {resp}");
                } catch (Exception e) {
                    resp = new Response() { success = false, message = "Bitte an Administrator wenden. Es ist ein fehler beim Spiel annehmen aufgetreten." }.ToString();
                    this.log($"Send answer: {resp}");
                }
            } else {
                resp = new Response() { success = false, message = "Session abgelaufen." }.ToString();
                this.log($"Send answer: {resp}");
            }

            return resp;
        }
        
        public string GiveUp(string sessionkey, string id) {
            string resp = "";

            //check if game id exists

            this.log($"Try to give up a game {id} from sessionkey {sessionkey}");
            if (this.IsValidSessionkey(sessionkey)) {
                try {
                    var command = new SQLiteCommand(this.connection);
                    command.CommandText = $"update game set status=7 where id={id} and player2={this.sessions[sessionkey].userid};";
                    command.ExecuteNonQuery();
                    command = new SQLiteCommand(this.connection);
                    command.CommandText = $"update game set status=6 where id={id} and player1={this.sessions[sessionkey].userid};";
                    command.ExecuteNonQuery();

                    this.KeepAlive(sessionkey);
                    resp = new Response() { success = true }.ToString();
                    this.log($"Send answer: {resp}");
                } catch (Exception e) {
                    resp = new Response() { success = false, message = "Bitte an Administrator wenden. Es ist ein fehler beim aufgeben aufgetreten." }.ToString();
                    this.log($"Send answer: {resp}");
                }
            } else {
                resp = new Response() { success = false, message = "Session abgelaufen." }.ToString();
                this.log($"Send answer: {resp}");
            }

            return resp;
        }

        public string GetGamefield(string sessionkey, string id) {
            string resp = "";

            //check if game id exists

            this.log($"Try to send gamefield {id} from sessionkey {sessionkey}");
            if (this.IsValidSessionkey(sessionkey)) {
                try {
                    List<Position> gamefield = new List<Position>();
                    var command = new SQLiteCommand(this.connection);
                    command.CommandText = $"select singlefield.koordx, singlefield.koordy, singlefield.status, game.player1 from singlefield inner join game on game.id=singlefield.game where game={id};";
                    SQLiteDataReader reader = command.ExecuteReader();
                    string player1ID = "";
                    while (reader.Read()) {
                        player1ID = reader[3].ToString();
                        gamefield.Add(new Position(Convert.ToInt32(reader[0].ToString()), Convert.ToInt32(reader[1].ToString()), Convert.ToInt32(reader[2].ToString())));
                    }
                    reader.Close();
                    reader.Dispose();
                    command.Dispose();

                    this.KeepAlive(sessionkey);
                    resp = new Response() { success = true, data = new Dictionary<string, object>() { { "IchBin", player1ID == this.sessions[sessionkey].userid.ToString() ? "1" : "2" }, { "gamefield", gamefield } } }.ToString();
                    this.log($"Send answer: {resp}");
                } catch (Exception e) {
                    resp = new Response() { success = false, message = "Bitte an Administrator wenden. Es ist ein fehler beim senden des Spielfelds aufgetreten." }.ToString();
                    this.log($"Send answer: {resp}");
                }
            } else {
                resp = new Response() { success = false, message = "Session abgelaufen." }.ToString();
                this.log($"Send answer: {resp}");
            }

            return resp;
        }

        public string SetTurn(string id, string sessionkey, string koordx, string koordy) {
            string resp = "";

            //check if game id exists

            this.log($"Try to set a turn in game {id} from sessionkey {sessionkey}");
            if (this.IsValidSessionkey(sessionkey)) {
                try {
                    var command = new SQLiteCommand(this.connection);
                    command.CommandText = $"select player1turn, player1 from game where id={id};";
                    SQLiteDataReader reader = command.ExecuteReader();
                    bool isTurned = false;
                    bool amiplayer1 = false;
                    bool isPlayer1Turn = false;
                    while (reader.Read()) {
                        bool isp1turn = reader[0].ToString() == "1";
                        if (isp1turn) {
                            if (reader[1].ToString() == this.sessions[sessionkey].userid.ToString())
                                isTurned = true;
                        } else {
                            if (reader[1].ToString() != this.sessions[sessionkey].userid.ToString())
                                isTurned = true;
                        }
                        if (reader[1].ToString() == this.sessions[sessionkey].userid.ToString())
                            amiplayer1 = true;
                        if (reader[0].ToString() == "1")
                            isPlayer1Turn = true;
                    }
                    reader.Close();
                    reader.Dispose();
                    command.Dispose();

                    if (isTurned) {
                        //check guilty try
                        command = new SQLiteCommand(this.connection);
                        command.CommandText = $"update singlefield set status={(amiplayer1 ? 1 : 2)} where game={id} and koordx={koordx} and koordy={koordy};";
                        command.ExecuteNonQuery();
                        command = new SQLiteCommand(this.connection);
                        command.CommandText = $"update game set player1turn={(isPlayer1Turn ? 0 : 1)} where id={id};";
                        command.ExecuteNonQuery();
                        this.KeepAlive(sessionkey);
                        resp = new Response() { success = true }.ToString();
                    } else {
                        resp = new Response() { success = false, message = "Du bist nicht an der Reihe." }.ToString();
                    }

                    this.log($"Send answer: {resp}");
                } catch (Exception e) {
                    resp = new Response() { success = false, message = "Bitte an Administrator wenden. Es ist ein fehler beim setzen aufgetreten." }.ToString();
                    this.log($"Send answer: {resp}");
                }
            } else {
                resp = new Response() { success = false, message = "Session abgelaufen." }.ToString();
                this.log($"Send answer: {resp}");
            }

            return resp;
        }
        
        public string SearchFriend(string sessionkey, string name) {
            string resp = "";

            //check if game id exists

            this.log($"Try to search friend {name} from sessionkey {sessionkey}");
            if (this.IsValidSessionkey(sessionkey)) {
                try {
                    List<SingleUser> data = new List<SingleUser>();
                    SQLiteCommand command = new SQLiteCommand(this.connection);
                    command.CommandText = $"SELECT id, name, alltimepoints, weeklypoints FROM account where name like '%{name}%'";
                    SQLiteDataReader reader = command.ExecuteReader();
                    int platz = 1;
                    while (reader.Read()) {
                        data.Add(new SingleUser(reader[0].ToString(), reader[1].ToString(), reader[2].ToString(), reader[3].ToString(), platz.ToString()));
                        platz++;
                    }
                    reader.Close();
                    reader.Dispose();
                    command.Dispose();

                    this.KeepAlive(sessionkey);
                    resp = new Response() { success = true, data = new Dictionary<string, object> { { "data", data } } }.ToString();
                    this.log($"Send answer: {resp}");
                } catch (Exception e) {
                    resp = new Response() { success = false, message = "Bitte an Administrator wenden. Es ist ein fehler beim suhen nach einem freund aufgetreten." }.ToString();
                    this.log($"Send answer: {resp}");
                }
            } else {
                resp = new Response() { success = false, message = "Session abgelaufen." }.ToString();
                this.log($"Send answer: {resp}");
            }

            return resp;
        }

        public string GetAllowOnlyFriend(string sessionkey) {
            string resp = "";

            this.log($"Try to get allowonlyfriends from sessionkey {sessionkey}");
            if (this.IsValidSessionkey(sessionkey)) {
                try {
                    SQLiteCommand command = new SQLiteCommand(this.connection);
                    command.CommandText = $"SELECT allowonlyfriends FROM account WHERE id={this.sessions[sessionkey].userid};";
                    SQLiteDataReader reader = command.ExecuteReader();
                    string allowsonlyfriends = "0";
                    while (reader.Read()) {
                        allowsonlyfriends = reader[0].ToString();
                    }
                    reader.Close();
                    reader.Dispose();
                    command.Dispose();

                    this.KeepAlive(sessionkey);
                    resp = new Response() { success = true, data = new Dictionary<string, object>() { { "allowonlyfriends", allowsonlyfriends } } }.ToString();
                    this.log($"Send answer: {resp}");
                } catch (Exception e) {
                    resp = new Response() { success = false, message = "Bitte an Administrator wenden. Es ist ein fehler beim getten von allowonlyfriends aufgetreten." }.ToString();
                    this.log($"Send answer: {resp}");
                }
            } else {
                resp = new Response() { success = false, message = "Session abgelaufen." }.ToString();
                this.log($"Send answer: {resp}");
            }

            return resp;
        }

        public string SetAllowOnlyFriends(string sessionkey, string allow) {
            string resp = "";

            this.log($"Try to set allowonlyfriends from sessionkey {sessionkey}");
            if (this.IsValidSessionkey(sessionkey)) {
                try {
                    SQLiteCommand command = new SQLiteCommand(this.connection);
                    command.CommandText = $"UPDATE account set allowonlyfriends={allow} WHERE id={this.sessions[sessionkey].userid};";
                    command.ExecuteNonQuery();

                    this.KeepAlive(sessionkey);
                    resp = new Response() { success = true }.ToString();
                    this.log($"Send answer: {resp}");
                } catch (Exception e) {
                    resp = new Response() { success = false, message = "Bitte an Administrator wenden. Es ist ein fehler beim setzen von allowonlyfriends aufgetreten." }.ToString();
                    this.log($"Send answer: {resp}");
                }
            } else {
                resp = new Response() { success = false, message = "Session abgelaufen." }.ToString();
                this.log($"Send answer: {resp}");
            }

            return resp;
        }
        #endregion

        /*
	- Create quick game: /game/createquick?session=KEY
	- Cancel search quick game: /game/cancelquick?session=KEY
	
    - Get Chat from game: /game/chat?id=ID&session=KEY
	- Send MSG: /game/send?id=ID&session=KEY
    
            CLEAN UP THREAD
             */

        private void InitDatabase(bool createNew) {
            if (!createNew && System.IO.File.Exists(Config.DATABASE_NAME)) {
                this.connection = new SQLiteConnection("Data Source=" + Config.DATABASE_NAME);
                this.connection.Open();
                return;
            }

            if (System.IO.File.Exists(Config.DATABASE_NAME))
                System.IO.File.Move(Config.DATABASE_NAME, $"{Config.DATABASE_NAME}{DateTime.Now.Ticks}");
            SQLiteConnection.CreateFile(Config.DATABASE_NAME);
            this.connection = new SQLiteConnection("Data Source=" + Config.DATABASE_NAME);
            this.connection.Open();

            SQLiteCommand command;

            //create account table
            command = new SQLiteCommand(this.connection);
            command.CommandText = "CREATE TABLE account (" +
                "id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                "name VARCHAR(64) NOT NULL, " +
                "alltimepoints INTEGER NOT NULL, " +
                "weeklypoints INTEGER NOT NULL, " +
                "mail VARCHAR(128) NOT NULL, " +
                "lastactivity VARCHAR(64) NOT NULL, " +
                "phone VARCHAR(32) NOT NULL, " +
                "allowonlyfriends INTEGER NOT NULL, " +
                "password VARCHAR(256) NOT NULL);";
            command.ExecuteNonQuery();

            //create account_account table
            command = new SQLiteCommand(this.connection);
            command.CommandText = "CREATE TABLE account_account (" +
                "idf INTEGER NOT NULL, " +
                "ids INTEGER NOT NULL);";
            command.ExecuteNonQuery();

            //create game table
            command = new SQLiteCommand(this.connection);
            command.CommandText = "CREATE TABLE game (" +
                "id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                "player1 INTEGER NOT NULL, " +
                "player2 INTEGER NOT NULL, " +
                "player1turn INTEGER NOT NULL, " +
                "newmsg INTEGER NOT NULL, " +
                "zugcount INTEGER NOT NULL, " +
                "playtime INTEGER NOT NULL, " +
                "lastturn VARCHAR(64) NOT NULL, " +
                "status INTEGER NOT NULL, " +
                "fieldsize INTEGER NOT NULL);";
            command.ExecuteNonQuery();

            //create singlefield table
            command = new SQLiteCommand(this.connection);
            command.CommandText = "CREATE TABLE singlefield (" +
                "id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                "game INTEGER NOT NULL, " +
                "koordx INTEGER NOT NULL, " +
                "koordy INTEGER NOT NULL, " +
                "status INTEGER NOT NULL);";
            command.ExecuteNonQuery();

            //create chat table
            command = new SQLiteCommand(this.connection);
            command.CommandText = "CREATE TABLE messages (" +
                "id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                "game INTEGER NOT NULL, " +
                "sender INTEGER NOT NULL, " +
                "status INTEGER NOT NULL, " +
                "text VARCHAR(256) NOT NULL, " +
                "time INTEGER NOT NULL);";
            command.ExecuteNonQuery();

            //create log table
            command = new SQLiteCommand(this.connection);
            command.CommandText = "CREATE TABLE log (" +
                "id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                "time VARCHAR(64) NOT NULL, " +
                "text VARCHAR(2048) NOT NULL);";
            command.ExecuteNonQuery();
        }

        #region helperfunctions
        private void log(string text) {
            var command = new SQLiteCommand(this.connection);
            command.CommandText = $"INSERT INTO log (time, text) VALUES ('{DateTime.Now}', '{text}');";
            command.ExecuteNonQuery();
        }
        public void addtestgames() {
            var command = new SQLiteCommand(this.connection);
            command.CommandText = $"INSERT INTO game (player1, player2, player1turn, newmsg, zugcount, playtime, lastturn, status, fieldsize) VALUES (1, 2, 1, 1, 25, 999, 43456478678, 3, 9);";
            command.ExecuteNonQuery();
            command = new SQLiteCommand(this.connection);
            command.CommandText = $"INSERT INTO game (player1, player2, player1turn, newmsg, zugcount, playtime, lastturn, status, fieldsize) VALUES (2, 1, 1, 1, 25, 999, 43456478678, 3, 9);";
            command.ExecuteNonQuery();
            command = new SQLiteCommand(this.connection);
            command.CommandText = $"INSERT INTO game (player1, player2, player1turn, newmsg, zugcount, playtime, lastturn, status, fieldsize) VALUES (1, 2, 1, 1, 25, 999, 43456478678, 3, 9);";
            command.ExecuteNonQuery();
            command = new SQLiteCommand(this.connection);
            command.CommandText = $"INSERT INTO game (player1, player2, player1turn, newmsg, zugcount, playtime, lastturn, status, fieldsize) VALUES (2, 1, 1, 1, 25, 999, 43456478678, 10, 9);";
            command.ExecuteNonQuery();
            command = new SQLiteCommand(this.connection);
            command.CommandText = $"INSERT INTO game (player1, player2, player1turn, newmsg, zugcount, playtime, lastturn, status, fieldsize) VALUES (1, 2, 1, 1, 25, 999, 43456478678, 9, 9);";
            command.ExecuteNonQuery();
            command = new SQLiteCommand(this.connection);
            command.CommandText = $"INSERT INTO game (player1, player2, player1turn, newmsg, zugcount, playtime, lastturn, status, fieldsize) VALUES (2, 1, 0, 1, 25, 999, 43456478678, 8, 9);";
            command.ExecuteNonQuery();
            command = new SQLiteCommand(this.connection);
            command.CommandText = $"INSERT INTO game (player1, player2, player1turn, newmsg, zugcount, playtime, lastturn, status, fieldsize) VALUES (1, 2, 0, 1, 25, 999, 43456478678, 7, 9);";
            command.ExecuteNonQuery();
            command = new SQLiteCommand(this.connection);
            command.CommandText = $"INSERT INTO game (player1, player2, player1turn, newmsg, zugcount, playtime, lastturn, status, fieldsize) VALUES (2, 1, 0, 1, 25, 999, 43456478678, 6, 9);";
            command.ExecuteNonQuery();
            command = new SQLiteCommand(this.connection);
            command.CommandText = $"INSERT INTO game (player1, player2, player1turn, newmsg, zugcount, playtime, lastturn, status, fieldsize) VALUES (1, 2, 0, 1, 25, 999, 43456478678, 5, 9);";
            command.ExecuteNonQuery();
            command = new SQLiteCommand(this.connection);
            command.CommandText = $"INSERT INTO game (player1, player2, player1turn, newmsg, zugcount, playtime, lastturn, status, fieldsize) VALUES (2, 1, 1, 1, 25, 999, 43456478678, 4, 9);";
            command.ExecuteNonQuery();
            command = new SQLiteCommand(this.connection);
            command.CommandText = $"INSERT INTO game (player1, player2, player1turn, newmsg, zugcount, playtime, lastturn, status, fieldsize) VALUES (1, 2, 1, 1, 25, 999, 43456478678, 2, 9);";
            command.ExecuteNonQuery();
            command = new SQLiteCommand(this.connection);
            command.CommandText = $"INSERT INTO game (player1, player2, player1turn, newmsg, zugcount, playtime, lastturn, status, fieldsize) VALUES (1, 2, 1, 1, 25, 999, 43456478678, 1, 9);";
            command.ExecuteNonQuery();
            command = new SQLiteCommand(this.connection);
            command.CommandText = $"INSERT INTO game (player1, player2, player1turn, newmsg, zugcount, playtime, lastturn, status, fieldsize) VALUES (2, 1, 1, 1, 25, 999, 43456478678, 1, 9);";
            command.ExecuteNonQuery();
        }

        private string CreateSessionKey() {
            List<string> characters = new List<string>() {
                "a", "A", "b", "B", "c", "C", "d", "D", "e", "E",
                "f", "F", "g", "G", "h", "H", "i", "I", "j", "J",
                "k", "K", "l", "L", "m", "M", "n", "N", "o", "O",
                "p", "P", "q", "Q", "r", "R", "s", "S", "t", "T",
                "u", "U", "v", "V", "w", "W", "x", "X", "y", "Y",
                "z", "Z",
                "0", "1", "2", "3", "4", "5", "6", "7", "8", "9"
            };

            StringBuilder sb = new StringBuilder("");
            Random rnd = new Random();

            for (int i = 0; i < Config.SESSION_KEY_LENGTH; i++)
                sb.Append(characters[rnd.Next(0, characters.Count)]);

            return sb.ToString();
        }

        private List<string> getFriends(string sessionkey) {
            List<string> friends = new List<string>();

            SQLiteCommand command = new SQLiteCommand(this.connection);
            command.CommandText = $"SELECT ids FROM account_account WHERE idf={this.sessions[sessionkey].userid};";
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read()) {
                friends.Add(reader[0].ToString());
            }
            reader.Close();
            reader.Dispose();
            command.Dispose();

            return friends;
        }

        private bool IsValidSessionkey(string key) {
            this.log($"Check Session Key {key}");
            if (this.sessions.ContainsKey(key)) {
                if (new TimeSpan(DateTime.Now.Ticks - this.sessions[key].lastactivity).TotalSeconds < Config.SESSION_DURATION) {
                    this.log($"Session Key {key} is valid.");
                    return true;
                } else {
                    this.log($"Session Key {key} is timed out");
                    return false;
                }
            } else {
                this.log($"Session Key {key} doesnt exist");
                return false;
            }
        }
        #endregion
    }
}