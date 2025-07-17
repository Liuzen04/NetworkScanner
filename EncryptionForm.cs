using System;
using System.Drawing;
using System.Windows.Forms;

namespace NetworkScanner
{
    /// <summary>
    /// Form mã hóa dữ liệu
    /// </summary>
    public class EncryptionForm : Form
    {
        private TextBox txtInput;
        private TextBox txtOutput;
        private TextBox txtPassword;
        private RadioButton rbEncrypt;
        private RadioButton rbDecrypt;
        private Button btnProcess;
        private Button btnCopy;
        private ComboBox cmbAlgorithm;

        public EncryptionForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Mã hóa dữ liệu mạng";
            this.Size = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var lblTitle = new Label
            {
                Text = "MÃ HÓA VÀ GIẢI MÃ DỮ LIỆU",
                Location = new Point(20, 20),
                Size = new Size(660, 30),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblAlgorithm = new Label
            {
                Text = "Thuật toán:",
                Location = new Point(20, 70),
                Size = new Size(80, 23)
            };

            cmbAlgorithm = new ComboBox
            {
                Location = new Point(105, 68),
                Size = new Size(150, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbAlgorithm.Items.AddRange(new[] { "AES" });
            cmbAlgorithm.SelectedIndex = 0;

            var lblPassword = new Label
            {
                Text = "Mật khẩu:",
                Location = new Point(280, 70),
                Size = new Size(70, 23)
            };

            txtPassword = new TextBox
            {
                Location = new Point(355, 68),
                Size = new Size(200, 23),
                PasswordChar = '●'
            };

            var chkShowPassword = new CheckBox
            {
                Text = "Hiện",
                Location = new Point(560, 70),
                Size = new Size(60, 23)
            };
            chkShowPassword.CheckedChanged += (s, e) =>
                txtPassword.PasswordChar = chkShowPassword.Checked ? '\0' : '●';

            var grpMode = new GroupBox
            {
                Text = "Chế độ",
                Location = new Point(20, 110),
                Size = new Size(200, 60)
            };

            rbEncrypt = new RadioButton
            {
                Text = "Mã hóa",
                Location = new Point(20, 25),
                Size = new Size(80, 23),
                Checked = true
            };

            rbDecrypt = new RadioButton
            {
                Text = "Giải mã",
                Location = new Point(110, 25),
                Size = new Size(80, 23)
            };

            grpMode.Controls.AddRange(new Control[] { rbEncrypt, rbDecrypt });

            var lblInput = new Label
            {
                Text = "Dữ liệu đầu vào:",
                Location = new Point(20, 190),
                Size = new Size(120, 23)
            };

            txtInput = new TextBox
            {
                Location = new Point(20, 215),
                Size = new Size(640, 100),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 10)
            };

            var lblOutput = new Label
            {
                Text = "Kết quả:",
                Location = new Point(20, 330),
                Size = new Size(120, 23)
            };

            txtOutput = new TextBox
            {
                Location = new Point(20, 355),
                Size = new Size(640, 100),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 10),
                BackColor = Color.FromArgb(245, 245, 245)
            };

            btnProcess = new Button
            {
                Text = "Thực hiện",
                Location = new Point(250, 110),
                Size = new Size(120, 40),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            btnCopy = new Button
            {
                Text = "Sao chép kết quả",
                Location = new Point(380, 110),
                Size = new Size(120, 40),
                BackColor = Color.FromArgb(52, 199, 89),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            var btnClear = new Button
            {
                Text = "Xóa",
                Location = new Point(510, 110),
                Size = new Size(80, 40),
                BackColor = Color.FromArgb(255, 59, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            // Event handlers
            btnProcess.Click += BtnProcess_Click;
            btnCopy.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(txtOutput.Text))
                {
                    Clipboard.SetText(txtOutput.Text);
                    MessageBox.Show("Đã sao chép kết quả!", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            btnClear.Click += (s, e) =>
            {
                txtInput.Clear();
                txtOutput.Clear();
                txtPassword.Clear();
            };

            this.Controls.AddRange(new Control[] {
                lblTitle, lblAlgorithm, cmbAlgorithm, lblPassword, txtPassword,
                chkShowPassword, grpMode, lblInput, txtInput, lblOutput, txtOutput,
                btnProcess, btnCopy, btnClear
            });
        }

        private void BtnProcess_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtInput.Text))
            {
                MessageBox.Show("Vui lòng nhập dữ liệu!", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Vui lòng nhập mật khẩu!", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (cmbAlgorithm.SelectedItem.ToString() == "AES")
                {
                    if (rbEncrypt.Checked)
                    {
                        txtOutput.Text = AESEncryption.Encrypt(txtInput.Text, txtPassword.Text);
                    }
                    else
                    {
                        txtOutput.Text = AESEncryption.Decrypt(txtInput.Text, txtPassword.Text);
                    }
                }               
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
