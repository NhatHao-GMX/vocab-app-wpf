using System;
using System.Collections.Generic;
using System.Windows;
using VocabApp.Data;
using VocabApp.Models;

namespace VocabAPPGUI
{
    public partial class MainWindow : Window
    {
        private Database _db;
        private List<dynamic> _dueWords;
        private int _currentIndex = 0;
        private bool _isMeaningShown = false;
        private dynamic _currentWord;

        // Quản lý Danh sách lỗi và Chuỗi streak
        private List<string> _forgottenWords;
        private int _sessionStreak = 0;

        public MainWindow()
        {
            InitializeComponent();
            _db = new Database();
            _forgottenWords = new List<string>();

            // Gán sự kiện cho các nút bấm
            btnShowMeaning.Click += BtnShowMeaning_Click;
            btnCorrect.Click += BtnCorrect_Click;
            btnIncorrect.Click += BtnIncorrect_Click;
            btnManage.Click += BtnManage_Click;
            btnRestart.Click += BtnRestart_Click;
            btnExit.Click += BtnExit_Click;

            LoadWords();
        }

        private void LoadWords()
        {
            try
            {
                var words = _db.GetDueWords();
                _dueWords = new List<dynamic>();
                foreach (var w in words)
                {
                    _dueWords.Add(w);
                }

                _currentIndex = 0;
                _sessionStreak = 0;
                _forgottenWords.Clear();

                // Trả UI về trạng thái ban đầu
                txtStreak.Text = $"Chuỗi: {_sessionStreak}";
                borderForgotBanner.Visibility = Visibility.Collapsed;
                panelInfo.Visibility = Visibility.Visible;
                btnCorrect.Visibility = Visibility.Visible;
                btnIncorrect.Visibility = Visibility.Visible;
                btnRestart.Visibility = Visibility.Collapsed;
                btnExit.Visibility = Visibility.Collapsed;

                ShowCurrentWord();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi cơ sở dữ liệu: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowCurrentWord()
        {
            // XỬ LÝ MÀN HÌNH KẾT THÚC BÀI HỌC
            if (_dueWords == null || _dueWords.Count == 0 || _currentIndex >= _dueWords.Count)
            {
                txtWord.Text = "HẾT TỪ! 🎉";
                panelInfo.Visibility = Visibility.Collapsed; // Ẩn IPA và loại từ
                txtExample.Text = "";

                // Hiện thông báo tổng kết trước, click vào ô này sẽ đổi thành "Tạm Biệt"
                string finishMessage = "Chúc mừng! Bạn đã hoàn thành xuất sắc phiên học từ vựng.";
                if (_forgottenWords.Count > 0)
                {
                    finishMessage += $"\n\nBạn đã sửa sai thành công {_forgottenWords.Count} từ chưa thuộc:\n" + string.Join(", ", _forgottenWords);
                }
                txtMeaning.Text = finishMessage;

                // THAY ĐỔI HỆ THỐNG NÚT BẤM THÀNH: ÔN LẠI HOẶC TẮT APP
                btnCorrect.Visibility = Visibility.Collapsed;
                btnIncorrect.Visibility = Visibility.Collapsed;
                btnRestart.Visibility = Visibility.Visible;
                btnExit.Visibility = Visibility.Visible;

                txtProgress.Text = $"Tiến độ: {_dueWords.Count}/{_dueWords.Count}";
                return;
            }

            _currentWord = _dueWords[_currentIndex];

            // Hiển thị tiến độ thực tế
            txtProgress.Text = $"Tiến độ: {_currentIndex + 1}/{_dueWords.Count}";

            txtWord.Text = _currentWord.English;
            txtIpa.Text = _currentWord.Ipa;
            txtPos.Text = $"{_currentWord.PartOfSpeech}";
            txtExample.Text = $"Ví dụ: {_currentWord.Example}";

            txtMeaning.Text = "Bấm để xem nghĩa tiếng Việt";
            _isMeaningShown = false;
        }

        private void BtnShowMeaning_Click(object sender, RoutedEventArgs e)
        {
            // KIỂM TRA: Nếu đã hết từ, click vào ô giữa sẽ chuyển thành chữ "Tạm Biệt"
            if (_dueWords == null || _dueWords.Count == 0 || _currentIndex >= _dueWords.Count)
            {
                txtMeaning.Text = "Tạm Biệt";
                return;
            }

            // Logic hiển thị nghĩa khi đang học bình thường
            if (_currentWord != null && !_isMeaningShown)
            {
                txtMeaning.Text = _currentWord.Vietnamese;
                _isMeaningShown = true;
            }
        }

        private void BtnCorrect_Click(object sender, RoutedEventArgs e)
        {
            if (_currentWord != null)
            {
                _db.LogReview(_currentWord.Id, 1);

                // Tăng chuỗi liên tiếp khi nhớ đúng
                _sessionStreak++;
                txtStreak.Text = $"Chuỗi: {_sessionStreak}";

                _currentIndex++;
                ShowCurrentWord();
            }
        }

        private void BtnIncorrect_Click(object sender, RoutedEventArgs e)
        {
            if (_currentWord != null)
            {
                _db.LogReview(_currentWord.Id, 0);

                // Reset chuỗi về 0 khi quên từ (Giống Duolingo)
                _sessionStreak = 0;
                txtStreak.Text = $"Chuỗi: {_sessionStreak}";

                // Thêm từ vào danh sách chưa thuộc (nếu chưa có)
                if (!_forgottenWords.Contains((string)_currentWord.English))
                {
                    _forgottenWords.Add((string)_currentWord.English);
                }

                // CẬP NHẬT BANNER ĐÚNG THEO YÊU CẦU CỦA BẠN
                borderForgotBanner.Visibility = Visibility.Visible;
                txtForgotBanner.Text = $"❌ Có {_forgottenWords.Count} từ chưa thuộc cần sửa sai cuối bài!";

                // THUẬT TOÁN VÒNG LẶP: Thêm từ hiện tại vào CUỐI DANH SÁCH để hỏi lại sau
                _dueWords.Add(_currentWord);

                _currentIndex++;
                ShowCurrentWord();
            }
        }

        private void BtnRestart_Click(object sender, RoutedEventArgs e)
        {
            LoadWords(); // Tải lại bài học mới
        }

        private void BtnManage_Click(object sender, RoutedEventArgs e)
        {
            // Mở cửa sổ quản lý dưới dạng Dialog (Hộp thoại)
            ManageWindow manageWindow = new ManageWindow();
            manageWindow.Owner = this; // Đặt MainWindow làm chủ để căn giữa theo MainWindow
            manageWindow.ShowDialog();

            // Sau khi đóng cửa sổ quản lý, tự động tải lại danh sách từ vựng ở màn hình chính
            LoadWords();
        }

        // Đã thêm hàm xử lý nút Thoát để hết lỗi CS0103
        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(); // Tắt hoàn toàn ứng dụng
        }
    }
}