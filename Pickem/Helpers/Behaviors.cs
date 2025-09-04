using Pickem.Helpers;

namespace Pickem.Behaviors;

public class UserChipBehavior : Behavior<ContentPage>
{
  protected override void OnAttachedTo(ContentPage page)
  {
    base.OnAttachedTo(page);
    // attach now and also whenever Title changes
    TitleHelper.AttachUserChip(page);
    page.PropertyChanged += PageOnPropertyChanged;
  }

  protected override void OnDetachingFrom(ContentPage page)
  {
    base.OnDetachingFrom(page);
    page.PropertyChanged -= PageOnPropertyChanged;
  }

  private static void PageOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
  {
    if (sender is ContentPage p && e.PropertyName == ContentPage.TitleProperty.PropertyName)
    {
      TitleHelper.AttachUserChip(p);
    }
  }
}
