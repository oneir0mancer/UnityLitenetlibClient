using LiteNetLib;

//Keep in mind that this is GameServer player
namespace NetworkingLayer.Client
{
    public class Player
    {
        public readonly byte Id;
        public int Ping;
        
        public readonly NetPeer AssociatedPeer;
        public string NickName;
        //Hashtable data
        
        public Player(NetPeer peer, string nickname = "")
        {
            Id = (byte)peer.Id;
            
            peer.Tag = this;
            AssociatedPeer = peer;
            NickName = nickname;
        }
        
        public Player(byte playerId, string nickname = "")
        {
            Id = playerId;
            NickName = nickname;
        }
    }
}