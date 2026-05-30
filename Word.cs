namespace VocabApp.Models;

public class Word
{
    public string? Ipa { get; set; }        // Thêm trường phiên âm (Ví dụ: /əˈbændən/)
    public string? ImagePath { get; set; }  // Thêm trường lưu đường dẫn ảnh máy tính
    public int Id { get; set; }
    public string English { get; set; } = "";
    public string Vietnamese { get; set; } = "";
    public string? Example { get; set; }
    public string? PartOfSpeech { get; set; }  // noun, verb, adj...

    // SM-2 Spaced Repetition fields
    public int Repetition { get; set; } = 0;       // số lần đã ôn đúng liên tiếp
    public double EaseFactor { get; set; } = 2.5;  // hệ số dễ (1.3 - 5.0)
    public int Interval { get; set; } = 1;          // khoảng cách ôn (ngày)
    public DateTime NextReview { get; set; } = DateTime.Today;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int TotalReviews { get; set; } = 0;
    public int CorrectReviews { get; set; } = 0;

    public double AccuracyRate => TotalReviews == 0 ? 0 : (double)CorrectReviews / TotalReviews * 100;
    public bool IsDue => NextReview.Date <= DateTime.Today;
}
