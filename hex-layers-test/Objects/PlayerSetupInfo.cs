using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexLayersTest.Objects;

public class PlayerSetupInfo
{
    public event Action<PlayerSetupInfo> Changed;

    private int _playerStockRemaining;
    public int StockRemaining
    {
        get => _playerStockRemaining;
        set
        {
            if (_playerStockRemaining != value)
            {
                _playerStockRemaining = value;
                Changed?.Invoke(this);
            }
        }
    }

    private Enums.UnitType _currentUnitType;
    public Enums.UnitType CurrentUnitType
    {
        get => _currentUnitType;
        set
        {
            if (_currentUnitType != value)
            {
                _currentUnitType = value;
                Changed?.Invoke(this);
            }
        }
    }

    private readonly Dictionary<Enums.UnitType, int> _playerUnitSupply = [];
    public IReadOnlyDictionary<Enums.UnitType, int> UnitSupply => _playerUnitSupply;

    public void SetUnitSupply(Enums.UnitType type, int amount)
    {
        _playerUnitSupply[type] = amount;
        Changed?.Invoke(this);
    }
}
