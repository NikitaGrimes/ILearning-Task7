namespace Task7.Entites
{
    public class Player
    {
        public string Name { get; set; }

        public Player Opponent { get; set; }

        public bool IsPlaying { get; set; }

        public bool WaitingForMove { get; set; }

        public bool IsSearchingOpponent { get; set; }

        public string ConnectionId { get; set; }

        public string Image { get; set; }
    }
}
