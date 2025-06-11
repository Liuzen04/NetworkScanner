using System;
using System.Windows.Forms;

namespace NetworkScanner
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Kiểm tra quyền Administrator
            if (!IsRunAsAdministrator())
            {
                var result = MessageBox.Show(
                    "Ứng dụng Network Scanner hoạt động tốt nhất khi chạy với quyền Administrator.\n\n" +
                    "Một số tính năng như lấy địa chỉ MAC có thể không hoạt động nếu không có quyền này.\n\n" +
                    "Bạn có muốn tiếp tục không?",
                    "Cảnh báo quyền hạn",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.No)
                {
                    return;
                }
            }

            Application.Run(new MainForm());
        }

        private static bool IsRunAsAdministrator()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }
}