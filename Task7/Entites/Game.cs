namespace Task7.Entites
{
    public class Game
    {
        public bool IsOver { get; private set; }

        public bool IsDraw { get; private set; }

        public Player Player1 { get; set; }

        public Player Player2 { get; set; }

        private readonly int[] field = new int[9];

        private int movesLeft = 9;

        public Game()
        {
            for (var i = 0; i < field.Length; i++)
            {
                field[i] = -1;
            }
        }

        public bool Play(int player, int position)
        {
            if (IsOver)
                return false;

            PlacePlayerNumber(player, position);
            return CheckWinner();
        }

        private bool CheckWinner()
        {
            for (int i = 0; i < 3; i++)
            {
                if (((field[i * 3] != -1 && field[(i * 3)] == field[(i * 3) + 1] && field[(i * 3)] == field[(i * 3) + 2]) ||
                     (field[i] != -1 && field[i] == field[i + 3] && field[i] == field[i + 6])))
                {
                    IsOver = true;
                    return true;
                }
            }

            if ((field[0] != -1 && field[0] == field[4] && field[0] == field[8]) || (field[2] != -1 && field[2] == field[4] && field[2] == field[6]))
            {
                IsOver = true;
                return true;
            }

            return false;
        }

        private void PlacePlayerNumber(int player, int position)
        {
            movesLeft--;

            if (movesLeft <= 0)
            {
                IsOver = true;
                IsDraw = true;
            }

            if (position < field.Length && field[position] == -1)
                field[position] = player;
        }
    }
}
