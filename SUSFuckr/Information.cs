using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms;

namespace SUSFuckr
{
    public static class Information
    {
        public static void ShowInfoWindow(string appVersion)
        {
            var infoForm = new InformationForm(appVersion);
            infoForm.ShowDialog();
        }
    }
}