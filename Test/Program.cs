using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BackEndLogic;
using Newtonsoft.Json;

namespace Test {
    class Program {
        static void Main(string[] args) {
            new IO();
            List<string> myOutput = new List<string>();

            myOutput.Add("Regist success: " + IO.Database.Registrieren("Testuser", "TestPW", "Testmail", "Testnummer"));
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            myOutput.Add("Regist nutzer belegt: " + IO.Database.Registrieren("Testuser2", "TestPW", "Testmail", "Testnummer"));
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            myOutput.Add("Regist success 2nd: " + IO.Database.Registrieren("Testuser", "TestPW", "Testmail", "Testnummer"));
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            
            myOutput.Add("Login failed: " + IO.Database.Login("Testuser", "falschesPW"));
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            myOutput.Add("Login successed: " + IO.Database.Login("Testuser", "TestPW"));
            string sessionkeyU1 = $"{(JsonConvert.DeserializeObject(myOutput[myOutput.Count - 1].Split(new string[] { ": " }, StringSplitOptions.None)[1]) as dynamic).data.SessionKey}";
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            myOutput.Add("Login successed: " + IO.Database.Login("Testuser2", "TestPW"));
            string sessionkeyU2 = $"{(JsonConvert.DeserializeObject(myOutput[myOutput.Count - 1].Split(new string[] { ": " }, StringSplitOptions.None)[1]) as dynamic).data.SessionKey}";
            Console.WriteLine(myOutput[myOutput.Count - 1]);

            myOutput.Add("Add Friend success: " + IO.Database.AddFriend(sessionkeyU1, "2"));
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            myOutput.Add("Remove Friend success: " + IO.Database.RemoveFriend(sessionkeyU1, "2"));
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            myOutput.Add("Add Friend success: " + IO.Database.AddFriend(sessionkeyU1, "2"));
            Console.WriteLine(myOutput[myOutput.Count - 1]);

            myOutput.Add("Get Friends success: " + IO.Database.Friends(sessionkeyU1));
            Console.WriteLine(myOutput[myOutput.Count - 1]);

            myOutput.Add("Invalid sessionkey: " + IO.Database.Ranks("Testtype", "InvalidsessionkeyU1"));
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            myOutput.Add("Wrong type rank fail: " + IO.Database.Ranks("Testtype", sessionkeyU1));
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            myOutput.Add("rank Weekly success: " + IO.Database.Ranks("weekly", sessionkeyU1));
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            myOutput.Add("rank alltime success: " + IO.Database.Ranks("alltime", sessionkeyU1));
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            myOutput.Add("rank friends success: " + IO.Database.Ranks("friends", sessionkeyU1));
            Console.WriteLine(myOutput[myOutput.Count - 1]);

            myOutput.Add("request a game success: " + IO.Database.RequestGame(sessionkeyU1, "2", "1", "9"));
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            myOutput.Add("request homescreen success: " + IO.Database.LoadHomeScreen(sessionkeyU1));
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            myOutput.Add("request reject game success: " + IO.Database.RejectGame(sessionkeyU1, "1"));
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            myOutput.Add("request homescreen success: " + IO.Database.LoadHomeScreen(sessionkeyU1));
            Console.WriteLine(myOutput[myOutput.Count - 1]);

            myOutput.Add("request a game success: " + IO.Database.RequestGame(sessionkeyU1, "2", "1", "9"));
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            myOutput.Add("request homescreen success: " + IO.Database.LoadHomeScreen(sessionkeyU1));
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            myOutput.Add("request decline game success: " + IO.Database.DeclineGame(sessionkeyU2, "2"));
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            myOutput.Add("request homescreen success: " + IO.Database.LoadHomeScreen(sessionkeyU1));
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            
            myOutput.Add("request a game success: " + IO.Database.RequestGame(sessionkeyU1, "2", "1", "9"));
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            myOutput.Add("request homescreen success: " + IO.Database.LoadHomeScreen(sessionkeyU1));
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            myOutput.Add("request giveup game success: " + IO.Database.GiveUp(sessionkeyU2, "3"));
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            myOutput.Add("request homescreen success: " + IO.Database.LoadHomeScreen(sessionkeyU1));
            Console.WriteLine(myOutput[myOutput.Count - 1]);

            myOutput.Add("request a game success: " + IO.Database.RequestGame(sessionkeyU2, "1", "1", "9"));
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            myOutput.Add("request homescreen success: " + IO.Database.LoadHomeScreen(sessionkeyU1));
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            myOutput.Add("request accept game success: " + IO.Database.AcceptGame(sessionkeyU1, "4"));
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            myOutput.Add("request homescreen success: " + IO.Database.LoadHomeScreen(sessionkeyU1));
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            myOutput.Add("request gamefield success: " + IO.Database.GetGamefield(sessionkeyU1, "4"));
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            myOutput.Add("request make turn fail: " + IO.Database.SetTurn("4", sessionkeyU2, "3", "5"));
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            myOutput.Add("request make turn success: " + IO.Database.SetTurn("4", sessionkeyU1, "3", "5"));
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            myOutput.Add("request gamefield success: " + IO.Database.GetGamefield(sessionkeyU1, "4"));
            Console.WriteLine(myOutput[myOutput.Count - 1]);
            myOutput.Add("request homescreen success: " + IO.Database.LoadHomeScreen(sessionkeyU1));
            Console.WriteLine(myOutput[myOutput.Count - 1]);

            System.IO.File.WriteAllLines("output.txt", myOutput);
            Console.ReadKey();
        }
    }
}
