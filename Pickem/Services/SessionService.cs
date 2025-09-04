using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Pickem.Services;

public class SessionService : INotifyPropertyChanged
{
  private string _userName = "Player";
  public string UserName
  {
    get => _userName;
    set { if (_userName != value) { _userName = value; OnPropertyChanged(); } }
  }

  public event PropertyChangedEventHandler? PropertyChanged;
  protected void OnPropertyChanged([CallerMemberName] string? name = null)
    => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
