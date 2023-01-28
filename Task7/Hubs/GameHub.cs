using Microsoft.AspNetCore.SignalR;
using Microsoft.VisualBasic;
using System.Collections.Concurrent;
using Task7.Entites;

namespace Task7.Hubs
{
    public class GameHub : Hub
    {
        public const string LoginComplete = "loginComplete";

        public const string WaitingForOpponent = "waitingForOpponent";

        public const string OpponentFound = "opponentFound";

        public const string OpponentNotFound = "opponentNotFound";

        public const string OpponentDisconnected = "opponentDisconnected";

        public const string WaitingForMove = "waitingForMove";

        public const string MoveMade = "moveMade";

        public const string GameOver = "gameOver";
        
        private static readonly ConcurrentBag<Player> players = new ConcurrentBag<Player>();

        private static readonly ConcurrentBag<Game> games = new ConcurrentBag<Game>();

        private static readonly Random coin = new Random();

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var game = games?.FirstOrDefault(j => j.Player1.ConnectionId == Context.ConnectionId || j.Player2.ConnectionId == Context.ConnectionId);
            if (game == null)
            {
                var playerWithoutGame = players.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
                if (playerWithoutGame != null)
                    Remove(players, playerWithoutGame);

                return base.OnDisconnectedAsync(exception); ;
            }

            if (game != null)
                Remove(games, game);

            var player = game.Player1.ConnectionId == Context.ConnectionId ? game.Player1 : game.Player2;

            if (player == null)
                return base.OnDisconnectedAsync(exception); ;

            Remove(players, player);
            if (player.Opponent != null)
                return OnOpponentDisconnected(player.Opponent.ConnectionId, player.Name);

            return base.OnDisconnectedAsync(exception);
        }

        public Task OnOpponentDisconnected(string connectionId, string playerName)
        {
            return Clients.Client(connectionId).SendAsync(OpponentDisconnected, playerName);
        }

        public void OnLoginComplete(string connectionId)
        {
            Clients.Client(connectionId).SendAsync(LoginComplete);
        }

        public void LeaveTheBattle()
        {
            var player = players?.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            var game = games?.FirstOrDefault(x => x.Player1.ConnectionId == Context.ConnectionId || x.Player2.ConnectionId == Context.ConnectionId);
            Remove(games, game);
            Clients.Client(game.Player1.ConnectionId).SendAsync(GameOver, $"Player {player.Name} leave the battle! The winner is {player.Opponent.Name}");
            Clients.Client(game.Player2.ConnectionId).SendAsync(GameOver, $"Player {player.Name} leave the battle! The winner is {player.Opponent.Name}");
            player.IsPlaying = false;
            player.Opponent.IsPlaying = false;
            Clients.Client(player.ConnectionId).SendAsync(LoginComplete);
            Clients.Client(player.Opponent.ConnectionId).SendAsync(LoginComplete);
        }

        public void LoginPlayer(string name)
        {
            var player = players?.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            if (player == null)
            {
                player = new Player { ConnectionId = Context.ConnectionId, Name = name, IsPlaying = false, IsSearchingOpponent = false};
                players.Add(player);
            }
            else
            {
                player.IsPlaying = false;
                player.IsSearchingOpponent = false;
            }

            OnLoginComplete(Context.ConnectionId);
        }

        public void MakeAMove(int position)
        {
            var game = games?.FirstOrDefault(x => x.Player1.ConnectionId == Context.ConnectionId || x.Player2.ConnectionId == Context.ConnectionId);

            if (game == null || game.IsOver)
                return;

            int symbol = 0;

            if (game.Player2.ConnectionId == Context.ConnectionId)
                symbol = 1;

            var player = symbol == 0 ? game.Player1 : game.Player2;

            if (player.WaitingForMove)
                return;

            Clients.Client(game.Player1.ConnectionId).SendAsync(MoveMade, player.Opponent.Name, position, player.Image);
            Clients.Client(game.Player2.ConnectionId).SendAsync(MoveMade, player.Opponent.Name, position, player.Image);

            if (game.Play(symbol, position))
            {
                Remove(games, game);
                Clients.Client(game.Player1.ConnectionId).SendAsync(GameOver, $"The winner is {player.Name}");
                Clients.Client(game.Player2.ConnectionId).SendAsync(GameOver, $"The winner is {player.Name}");
                player.IsPlaying = false;
                player.Opponent.IsPlaying = false;
                Clients.Client(player.ConnectionId).SendAsync(LoginComplete);
                Clients.Client(player.Opponent.ConnectionId).SendAsync(LoginComplete);
            }

            if (game.IsOver && game.IsDraw)
            {
                Remove(games, game);
                Clients.Client(game.Player1.ConnectionId).SendAsync(GameOver, "It's a draw!!!");
                Clients.Client(game.Player2.ConnectionId).SendAsync(GameOver, "It's a draw!!!");
                player.IsPlaying = false;
                player.Opponent.IsPlaying = false;
                Clients.Client(player.ConnectionId).SendAsync(LoginComplete);
                Clients.Client(player.Opponent.ConnectionId).SendAsync(LoginComplete);
            }

            if (!game.IsOver)
            {
                player.WaitingForMove = !player.WaitingForMove;
                player.Opponent.WaitingForMove = !player.Opponent.WaitingForMove;
            }
        }

        public void FindOpponent()
        {
            var player = players.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            if (player == null)
                return;

            player.IsPlaying = false;
            player.IsSearchingOpponent = true;
            var opponent = players.Where(x => x.ConnectionId != Context.ConnectionId && x.IsSearchingOpponent && !x.IsPlaying).FirstOrDefault();
            if (opponent == null)
            {
                Clients.Client(Context.ConnectionId).SendAsync(OpponentNotFound);
                return;
            }

            player.IsPlaying = true;
            player.IsSearchingOpponent = false;
            opponent.IsPlaying = true;
            opponent.IsSearchingOpponent = false;
            player.Opponent = opponent;
            opponent.Opponent = player;
            bool isFirst = coin.Next(0, 1) == 0;
            if (isFirst)
            {
                player.Image = "cross.png";
                opponent.Image = "circle.png";
            }
            else
            {
                player.Image = "circle.png";
                opponent.Image = "cross.png";
            }
            Clients.Client(Context.ConnectionId).SendAsync(OpponentFound, opponent.Name, opponent.Image, player.Image);
            Clients.Client(opponent.ConnectionId).SendAsync(OpponentFound, player.Name, player.Image, opponent.Image);

            if (isFirst)
            {
                player.WaitingForMove = false;
                opponent.WaitingForMove = true;
                Clients.Client(player.ConnectionId).SendAsync(WaitingForMove, opponent.Name);
                Clients.Client(opponent.ConnectionId).SendAsync(WaitingForOpponent, player.Name);
            }
            else
            {
                player.WaitingForMove = true;
                opponent.WaitingForMove = false;
                Clients.Client(opponent.ConnectionId).SendAsync(WaitingForMove, player.Name);
                Clients.Client(player.ConnectionId).SendAsync(WaitingForOpponent, opponent.Name);
            }

            games.Add(new Game { Player1 = player, Player2 = opponent });
        }

        private void Remove<T>(ConcurrentBag<T> players, T playerWithoutGame)
        {
            players = new ConcurrentBag<T>(players?.Except(new[] { playerWithoutGame }));
        }
    }
}
