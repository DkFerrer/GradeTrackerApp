using GradeTrackerApp.Views;

namespace GradeTrackerApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();


            Routing.RegisterRoute("HomePage", typeof(HomePage));
            Routing.RegisterRoute("SubjectsPage", typeof(SubjectsPage));
            Routing.RegisterRoute("PredictorPage", typeof(PredictorPage));
            Routing.RegisterRoute("ProfilePage", typeof(ProfilePage));


        }
    }
}
