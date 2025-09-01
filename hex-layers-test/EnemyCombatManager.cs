using Godot;
using HexLayersTest.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexLayersTest;

public class EnemyCombatManager
{
    private readonly LevelArray _level;
    private readonly List<Team> _playerTeams;
    private readonly List<Team> _cpuTeams;

    private IEnumerable<Unit> PlayerUnits => _playerTeams.SelectMany(t => t.Units);
    private IEnumerable<Unit> CPUUnits => _cpuTeams.SelectMany(_t => _t.Units);

    public EnemyCombatManager(LevelArray level, IEnumerable<Team> teams)
    {
        _level = level;
        _playerTeams = [];
        _cpuTeams = [];

        foreach (Team team in teams)
        {
            if (team.IsPlayerControlled) _playerTeams.Add(team);
            else _cpuTeams.Add(team);
        }
    }

    public Task<Dictionary<Unit, Vector2I>> GenerateMovesAsync()
    {
        return Task.Run(() =>
        {
            return CPUUnits.ToDictionary(unit => unit, unit => new Vector2I(unit.GridPosition.X, unit.GridPosition.Y + 1));
        });
    }
}
