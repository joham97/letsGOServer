using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server {
    class Program {
        static void Main(string[] args) {
            new BackEndLogic.IO();

            TcpListener server = new TcpListener(8080);
            server.Start();

            while (true) {
                TcpClient client = server.AcceptTcpClient();
                new Thread(() => {
                    var tempClient = client;
                    try {
                        StreamReader sr = new StreamReader(tempClient.GetStream());
                        List<string> myGetRequests = new List<string>();
                        string lel = "test";
                        while (lel != "") {
                            lel = sr.ReadLine();
                            if (lel.Contains("GET"))
                                myGetRequests.Add(lel.Split(' ')[1]);
                        }

                        string answer = "";
                        try {
                            foreach (var a in myGetRequests) {
                                Dictionary<string, string> data = new Dictionary<string, string>();
                                foreach (var b in a.Substring(a.IndexOf('?') + 1).Split('&')) {
                                    data.Add(b.Split('=')[0], b.Split('=')[1]);
                                }
                                switch (a.Substring(0, a.IndexOf('?'))) {
                                    case "/account/reg":
                                        answer = BackEndLogic.IO.Database.Registrieren(data["user"], data["password"], data["mail"], data["phone"]);
                                        break;
                                    case "/account/login":
                                        answer = BackEndLogic.IO.Database.Login(data["user"], data["password"]);
                                        break;
                                    case "/player/friends":
                                        answer = BackEndLogic.IO.Database.Friends(data["sessionkey"]);
                                        break;
                                    case "/player/search":
                                        answer = BackEndLogic.IO.Database.SearchFriend(data["sessionkey"], data["name"]);
                                        break;
                                    case "/player/addfriend":
                                        answer = BackEndLogic.IO.Database.AddFriend(data["sessionkey"], data["userid"]);
                                        break;
                                    case "/player/removefriend":
                                        answer = BackEndLogic.IO.Database.RemoveFriend(data["sessionkey"], data["userid"]);
                                        break;
                                    case "/player/homescreen":
                                        answer = BackEndLogic.IO.Database.LoadHomeScreen(data["sessionkey"]);
                                        break;
                                    case "/player/ranking":
                                        answer = BackEndLogic.IO.Database.Ranks(data["type"], data["sessionkey"]);
                                        break;
                                    case "/player/keepalive":
                                        answer = BackEndLogic.IO.Database.KeepAlive(data["sessionkey"]);
                                        break;
                                    case "/player/getallowonlyfriends":
                                        answer = BackEndLogic.IO.Database.GetAllowOnlyFriend(data["sessionkey"]);
                                        break;
                                    case "/player/setallowonlyfriendss":
                                        answer = BackEndLogic.IO.Database.SetAllowOnlyFriends(data["sessionkey"], data["allowonlyfriends"]);
                                        break;
                                    case "/game/create":
                                        answer = BackEndLogic.IO.Database.RequestGame(data["sessionkey"], data["userid"], data["time"], data["size"]);
                                        break;
                                    case "/game/accept":
                                        answer = BackEndLogic.IO.Database.AcceptGame(data["sessionkey"], data["id"]);
                                        break;
                                    case "/game/decline":
                                        answer = BackEndLogic.IO.Database.DeclineGame(data["sessionkey"], data["id"]);
                                        break;
                                    case "/game/field":
                                        answer = BackEndLogic.IO.Database.GetGamefield(data["sessionkey"], data["id"]);
                                        break;
                                    case "/game/turn":
                                        answer = BackEndLogic.IO.Database.SetTurn(data["id"], data["sessionkey"], data["x"], data["y"]);
                                        break;
                                    case "/game/concede":
                                        answer = BackEndLogic.IO.Database.GiveUp(data["sessionkey"], data["id"]);
                                        break;
                                    default:
                                        Console.WriteLine(a.Substring(0, a.IndexOf('?')));
                                        break;
                                }

                            }
                        } catch {
                            answer = new BackEndLogic.Response() { success = false, message = "Nicht alle benötigten Daten übergeben." }.ToString() + "  ";
                        }

                        StreamWriter writer = new StreamWriter(tempClient.GetStream());
                        writer.Write("HTTP/1.0 200 OK");
                        writer.Write(Environment.NewLine);
                        writer.Write("Content-Type: text/plain; charset=UTF-8");
                        writer.Write(Environment.NewLine);
                        writer.Write("Content-Length: " + answer.Length);
                        writer.Write(Environment.NewLine);
                        writer.Write(Environment.NewLine);
                        writer.Write(answer);
                        writer.Flush();
                    } catch {

                    } finally {
                        tempClient.Close();
                    }
                }).Start();
            }
        }
    }
}
