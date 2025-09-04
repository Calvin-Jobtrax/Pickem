using System.Collections.ObjectModel;

namespace Pickem.Models
{
  public sealed class PoolGroup : ObservableCollection<PoolItem>
  {
    public string DateHeader { get; }
    public PoolGroup(string header, IEnumerable<PoolItem> items) : base(items)
      => DateHeader = header;
  }
}
