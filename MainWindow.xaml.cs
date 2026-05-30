using System;
using System.Windows;
using System.Windows.Controls;
using VocabApp.Data;
using VocabApp.Models;

namespace VocabAPPGUI
{
    public partial class ManageWindow : Window
    {
        private Database _db;
        private int _selectedWordId = -1;

        public ManageWindow()
        {
            InitializeComponent();
            _db = new Database();

            // Đấu dây sự kiện cho các nút bấm
            btnAdd.Click += BtnAdd_Click;
            btnUpdate.Click += BtnUpdate_Click;
            btnDelete.Click += BtnDelete_Click;
            btnClear.Click += BtnClear_Click;

            // 📁 ĐẤU DÂY CHO NÚT CHÈN FILE MỚI Ở ĐÂY NÈ CẬU
            btnImportFile.Click += BtnImportFile_Click;

            RefreshGrid();
        }

        private void RefreshGrid()
        {
            try
            {
                var allWords = _db.GetAllWords();
                dgWords.ItemsSource = allWords;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tải danh sách từ vựng: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DgWords_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgWords.SelectedItem is Word selectedWord)
            {
                _selectedWordId = selectedWord.Id;

                txtEnglish.Text = selectedWord.English;
                txtIpa.Text = selectedWord.Ipa;
                txtPos.Text = selectedWord.PartOfSpeech;
                txtVietnamese.Text = selectedWord.Vietnamese;
                txtExample.Text = selectedWord.Example;
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtEnglish.Text) || string.IsNullOrWhiteSpace(txtVietnamese.Text))
            {
                MessageBox.Show("Vui lòng điền tối thiểu Từ tiếng Anh và Nghĩa tiếng Việt!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string eng = txtEnglish.Text.Trim();
                string ipa = txtIpa.Text.Trim();
                string pos = txtPos.Text.Trim();
                string viet = txtVietnamese.Text.Trim();
                string ex = txtExample.Text.Trim();

                _db.AddWord(eng, ipa, pos, viet, ex);
                MessageBox.Show("Thêm từ vựng thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                ClearInputs();
                RefreshGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi thêm từ: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedWordId == -1)
            {
                MessageBox.Show("Vui lòng chọn một từ vựng trong bảng bên dưới để sửa!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string eng = txtEnglish.Text.Trim();
                string ipa = txtIpa.Text.Trim();
                string pos = txtPos.Text.Trim();
                string viet = txtVietnamese.Text.Trim();
                string ex = txtExample.Text.Trim();

                _db.UpdateWord(_selectedWordId, eng, ipa, pos, viet, ex);
                MessageBox.Show("Cập nhật thông tin thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                ClearInputs();
                RefreshGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi cập nhật từ: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedWordId == -1)
            {
                MessageBox.Show("Vui lòng chọn một từ vựng trong bảng trước để xóa!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirmResult = MessageBox.Show($"Bạn có chắc chắn muốn xóa từ '{txtEnglish.Text}' khỏi kho lưu trữ không?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirmResult == MessageBoxResult.Yes)
            {
                try
                {
                    _db.DeleteWord(_selectedWordId);
                    MessageBox.Show("Đã xóa từ vựng thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    ClearInputs();
                    RefreshGrid();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi xóa từ: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearInputs();
        }

        // 📁 ĐOẠN XỬ LÝ ĐỌC FILE TEXT KHI BẤM NÚT TÍM
        private void BtnImportFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "Chọn file từ vựng tiếng Anh của bạn"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    _db.ImportNaturalTextFile(openFileDialog.FileName);
                    MessageBox.Show("Đã nạp toàn bộ từ vựng từ file text vào kho thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    RefreshGrid();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi đọc file: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ClearInputs()
        {
            _selectedWordId = -1;
            txtEnglish.Clear();
            txtIpa.Clear();
            txtPos.Clear();
            txtVietnamese.Clear();
            txtExample.Clear();
            dgWords.SelectedItem = null;
        }
    }
}