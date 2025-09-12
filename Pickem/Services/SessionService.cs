using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Storage;

namespace Pickem.Services;

public class SessionService : INotifyPropertyChanged
{
    private const string KeyPlayerId = "session.playerId";
    private const int DefaultPlayerId = 1;

    private string _userName = "Player";
    public string UserName
    {
        get => _userName;
        set
        {
            if (_userName != value)
            {
                _userName = value;
                OnPropertyChanged();
            }
        }
    }

    private int _playerId;
    public int PlayerId
    {
        get => _playerId;
        set
        {
            if (_playerId != value)
            {
                _playerId = (value > 0 ? value : DefaultPlayerId);
                Preferences.Set(KeyPlayerId, _playerId); // persist
                OnPropertyChanged();
            }
        }
    }

    public SessionService()
    {
        // Initialize from persisted preferences or fallback
        _playerId = Preferences.Get(KeyPlayerId, DefaultPlayerId);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
