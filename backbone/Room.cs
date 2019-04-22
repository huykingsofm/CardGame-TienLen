class Room{
    int[] player = null;
    Game game = null;
    int[] ReadyStatus = null; // -1 - người chơi ko tồn tại,  0 - chưa sẵn sàng,  1 - sẵn sàng

    public Room(){
        // 
        player = new ClientSession[4];
        ReadyStatus = new ReadyStatus[4];
        for(int i = 0; i < 4; i++){
            ReadyStatus[i] = -1;
        }
    }   

    public Room(ClientSession player) : Room(){
        this.player[0] = player;
        this.ReadyStatus[0] = 0;
        this.player[0]
    }
}