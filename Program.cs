using System;
using System.Windows.Forms;

namespace XYZRenderLauncher
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            SplashScreen splash = new SplashScreen();
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 5000;
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                splash.Close();
            };

            timer.Start();
            splash.ShowDialog();
            Application.Run(new XYZ());
        }
    }
}
