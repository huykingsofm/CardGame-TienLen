using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client {
    public class RequestFormat {
        public static String[] SPADE = { "0_0", "0_0", "0_0", "3_1", "4_1", "5_1", "6_1", "7_1",
                          "8_1", "9_1", "10_1", "11_1", "12_1", "13_1", "14_1", "15_1" };

        public static String[] CLUBS = { "0_0", "0_0", "0_0", "3_2", "4_2", "5_2", "6_2", "7_2",
                          "8_2", "9_2", "10_2", "11_2", "12_2", "13_2", "14_2", "15_2" };

        public static String[] DIAMONDS = { "0_0", "0_0", "0_0", "3_3", "4_3", "5_3", "6_3", "7_3",
                          "8_3", "9_3", "10_3", "11_3", "12_3", "13_3", "14_3", "15_3" };

        public static String[] HEARTS = { "0_0", "0_0", "0_0", "3_4", "4_4", "5_4", "6_4", "7_4",
                          "8_4", "9_4", "10_4", "11_4", "12_4", "13_4", "14_4", "15_4" };
        public static String SIGN_UP(String uname, String pass) {
            return "Signup:" + uname + "," + pass;
        }
        public static String LOG_IN(String uname, String pass) {
            return "Login:" + uname + "," + pass;
        }
        public static String DISCONNECT() { return "DISC"; }
        public static String GET_ROOMS() { return "GETR"; }
        public static String EXIT_ROOM() { return "JoinLobby"; }
        public static String EXIT_LOBBY() { return "Logout"; }
        public static String JOIN_ROOM(int index) { return ("JoinRoom:" + index); }
        public static String GET_LOBBY_INFO() { return "LobbyInfo"; }
        public static String NEXT_TURN() { return "NEXT"; }
        public static String READY_GAME() { return "Ready"; }
        public static String UNREADY_GAME() { return "UnReady"; }
        public static String START_GAME() { return "Start"; }
        public static String PLAY(String cards) { return "Play:" + cards; }
        public static String PLAY(String[] cards) {
            int length = cards.Length;
            String req = "Play:";
            for(int i = 0; i < length; i++) {
                req += cards[i];
                if(i < length - 1) {
                    req += ",";
                }
            }

            return req;
        }
        public static String SKIP() { return "Pass"; }
        public static String SET_AI(int index) { return "SetAI:" + index; }
        public static String REMOVE_AI(int index) { return "RemoveAI:" + index; }
        public static String PAY_IN(String code) { return "Payin:" + code; }
    }
}
