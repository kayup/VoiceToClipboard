using VoiceToClipboard.Views;

namespace VoiceToClipboard
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new VoiceWindow();
        }
    }
}
