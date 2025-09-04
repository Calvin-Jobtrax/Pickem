namespace Pickem.Pages;

public partial class LoginPage : ContentPage
{
  public LoginPage()
  {
    InitializeComponent();
  }

  void Username_Completed(object sender, EventArgs e) => PasswordEntry?.Focus();
}
