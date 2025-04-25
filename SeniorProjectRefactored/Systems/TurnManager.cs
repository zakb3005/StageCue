using System;
using System.Collections.Generic;
using System.Linq;
using SeniorProjectRefactored.Helpers;
using SeniorProjectRefactored.Networking;

namespace SeniorProjectRefactored.Systems
{
    public class TurnManager
    {
        public bool IsHostTurn { get; private set; } = true;

        public Player CurrentPlayer => IsHostTurn ? null : (_players.Count > 0 && _currentPlayerIndex < _players.Count ? _players[_currentPlayerIndex] : null);
        public string CurrentPlayerId { get; private set; } = "HOST";

        private readonly List<Player> _players = new List<Player>();
        private int _currentPlayerIndex = 0;

        public event Action OnHostTurnStarted;
        public event Action<Player> OnClientTurnStarted;

        public void AddPlayer(Player p)
        {
            if (p == null || _players.Exists(pl => pl.ID == p.ID)) return;
            _players.Add(p);
        }

        public void RemovePlayer(string id)
        {
            var pl = _players.FirstOrDefault(p => p.ID == id);
            if (pl == null) return;

            int removedIndex = _players.IndexOf(pl);
            bool removedWasCurrent = !IsHostTurn && removedIndex == _currentPlayerIndex;

            _players.RemoveAt(removedIndex);

            if (_players.Count == 0)
            {
                IsHostTurn = true;
                CurrentPlayerId = "HOST";
                _currentPlayerIndex = 0;
                OnHostTurnStarted?.Invoke();
                return;
            }

            if (removedWasCurrent)
            {
                IsHostTurn = true;
                CurrentPlayerId = "HOST";
                _currentPlayerIndex = 0;
                OnHostTurnStarted?.Invoke();
            }
            else if (_currentPlayerIndex >= _players.Count)
            {
                _currentPlayerIndex = 0;
                CurrentPlayerId = _players[_currentPlayerIndex].ID;
                OnClientTurnStarted?.Invoke(_players[_currentPlayerIndex]);
            }
        }

        public List<Player> GetPlayers() => _players;

        public void NextTurn()
        {
            if (IsHostTurn)
            {
                if (_players.Count > 0)
                {
                    IsHostTurn = false;

                    if (_currentPlayerIndex >= _players.Count)
                        _currentPlayerIndex = 0;

                    CurrentPlayerId = _players[_currentPlayerIndex].ID;
                    OnClientTurnStarted?.Invoke(_players[_currentPlayerIndex]);
                }
                else
                {
                    OnHostTurnStarted?.Invoke();
                }
            }
            else
            {
                IsHostTurn = true;
                CurrentPlayerId = "HOST";
                OnHostTurnStarted?.Invoke();

                if (_players.Count > 0)
                    _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Count;
            }
        }

        public void RemoteAdvance(TurnUpdateMsg msg)
        {
            if (msg == null) return;

            IsHostTurn = msg.IsHostTurn;
            CurrentPlayerId = msg.CurrentPlayerId ?? "HOST";

            if (IsHostTurn)
            {
                OnHostTurnStarted?.Invoke();
            }
            else
            {
                int idx = _players.FindIndex(p => p.ID == CurrentPlayerId);
                _currentPlayerIndex = idx >= 0 ? idx : 0;
                OnClientTurnStarted?.Invoke(CurrentPlayer);
            }
        }

        public bool IsMyTurn(string myId)
        {
            return (IsHostTurn && myId == "HOST") || (!IsHostTurn && CurrentPlayerId == myId);
        }
    }
}