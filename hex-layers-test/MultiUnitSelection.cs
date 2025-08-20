using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexLayersTest;

public class MultiUnitSelection : IEnumerable<Player>
{
    private readonly HashSet<Player> _players;
    private int _minX, _minY, _maxX, _maxY;

    public MultiUnitSelection()
    {
        _players = [];
    }

    public void Clear()
    {
        _players.Clear();
        _minX = 0;
        _minY = 0;
        _maxX = 0;
        _maxY = 0;
    }

    public int Count => _players.Count;

    public bool Add(Player player)
    {
        
        if (_players.Count == 0 )
        {
            (_minX, _minY) = player.GridPosition;
            (_maxX, _maxY) = (_minX, _minY);
        }
        else
        {
            _minX = Math.Min(player.GridPosition.X, _minX);
            _minY = Math.Min(player.GridPosition.Y, _minY);
            _maxX = Math.Max(player.GridPosition.X, _maxX);
            _maxY = Math.Max(player.GridPosition.Y, _maxY);
        }
            
        return _players.Add(player);
    }

    public bool Contains(Player player) => _players.Contains(player);

    public Player[,] GetFormation()
    {
        if (_players.Count == 0) return null;

        var formation = new Player[_maxX -  _minX + 1, _maxY - _minY + 1];

        foreach ( var player in _players)
            formation[player.GridPosition.X - _minX, player.GridPosition.Y - _minY] = player;

        return formation;
    }

    public IEnumerator<Player> GetEnumerator() => _players.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
