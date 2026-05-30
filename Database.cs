using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using VocabApp.Models;

namespace VocabApp.Data;

public class Database : IDisposable
{
    private readonly SqliteConnection _connection;

    public Database(string dbPath = "vocab.db")
    {
        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();
        InitializeSchema();
        SeedSampleData();
    }

    private void InitializeSchema()
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Words (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                English         TEXT NOT NULL,
                Vietnamese      TEXT NOT NULL,
                Example         TEXT,
                PartOfSpeech    TEXT,
                Repetition      INTEGER NOT NULL DEFAULT 0,
                EaseFactor      REAL    NOT NULL DEFAULT 2.5,
                Interval        INTEGER NOT NULL DEFAULT 1,
                NextReview      TEXT    NOT NULL DEFAULT (date('now')),
                CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
                TotalReviews    INTEGER NOT NULL DEFAULT 0,
                CorrectReviews  INTEGER NOT NULL DEFAULT 0,
                Ipa             TEXT,
                ImagePath       TEXT
            );
            CREATE TABLE IF NOT EXISTS ReviewLog (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                WordId      INTEGER NOT NULL,
                Quality     INTEGER NOT NULL,
                ReviewedAt  TEXT    NOT NULL DEFAULT (datetime('now')),
                FOREIGN KEY (WordId) REFERENCES Words(Id)
            );
        ";
        cmd.ExecuteNonQuery();

        // 🛠️ TỰ ĐỘNG CẬP NHẬT: Thêm cột Ipa và ImagePath nếu file database cũ chưa có
        try
        {
            var alterIpa = _connection.CreateCommand();
            alterIpa.CommandText = "ALTER TABLE Words ADD COLUMN Ipa TEXT;";
            alterIpa.ExecuteNonQuery();
        }
        catch { /* Đã có cột rồi thì bỏ qua không báo lỗi */ }

        try
        {
            var alterImg = _connection.CreateCommand();
            alterImg.CommandText = "ALTER TABLE Words ADD COLUMN ImagePath TEXT;";
            alterImg.ExecuteNonQuery();
        }
        catch { /* Đã có cột rồi thì bỏ qua không báo lỗi */ }
    }

    private void SeedSampleData()
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Words";
        var count = (long)(cmd.ExecuteScalar() ?? 0);
        if (count > 0) return;

        // 🔄 ĐÃ CẬP NHẬT: Thêm sẵn toàn bộ phiên âm IPA chuẩn quốc tế cho các từ vựng mẫu
        var sampleWords = new[]
        {
            ("abandon",     "əˈbændən",    "từ bỏ",        "She decided to abandon the project.",              "verb"),
            ("ambiguous",   "æmˈbɪɡjuəs",   "mơ hồ",        "His answer was ambiguous and confusing.",           "adj"),
            ("benevolent",  "bəˈnevələnt",  "nhân từ",      "She was a benevolent leader.",                      "adj"),
            ("candid",      "ˈkændɪd",     "thẳng thắn",   "Please be candid about your opinion.",              "adj"),
            ("diligent",    "ˈdɪlɪdʒənt",   "chăm chỉ",      "He is a diligent student.",                         "adj"),
            ("eloquent",    "ˈeləkwənt",    "hùng hồn",     "She gave an eloquent speech.",                      "adj"),
            ("fluctuate",   "ˈflʌktʃueɪt",  "dao động",     "Prices tend to fluctuate during inflation.",         "verb"),
            ("gratitude",   "ˈɡrætɪtjuːd",  "lòng biết ơn", "She expressed her gratitude sincerely.",             "noun"),
            ("hypothesis",  "haɪˈpɒθəsɪs",  "giả thuyết",    "The scientist formed a hypothesis.",                "noun"),
            ("inevitable",  "ɪnˈevɪtəbl",   "không thể tránh","Change is inevitable in life.",                   "adj"),
            ("jeopardize",  "ˈdʒepədaɪz",   "gây nguy hiểm","Don't jeopardize your health for work.",            "verb"),
            ("keen",        "kiːn",         "nhạy bén",     "She has a keen eye for detail.",                    "adj"),
            ("lethargic",   "ləˈθɑːdʒɪk",   "uể oải",       "He felt lethargic after the heavy meal.",           "adj"),
            ("meticulous",  "məˈtɪkjələs",  "tỉ mỉ",        "She is meticulous in her work.",                    "adj"),
            ("notorious",   "nəʊˈtɔːriəs",  "khét tiếng",   "He became notorious for his crimes.",               "adj"),
            ("obsolete",    "ˈɒbsəliːt",    "lỗi thời",     "That technology is now obsolete.",                  "adj"),
            ("persevere",   "ˌpɜːsɪˈvɪə",   "kiên trì",     "You must persevere despite difficulties.",           "verb"),
            ("quarantine",  "ˈkwɒrəntiːn",  "cách ly",      "They had to quarantine for two weeks.",              "verb"),
            ("resilient",   "rɪˈzɪliənt",   "kiên cường",   "Children are often very resilient.",                "adj"),
            ("scrutinize",  "ˈskruːtənaɪz", "xem xét kỹ",  "He scrutinized every detail of the contract.",     "verb"),
        };

        foreach (var (eng, ipa, viet, ex, pos) in sampleWords)
        {
            var insertCmd = _connection.CreateCommand();
            insertCmd.CommandText = @"
                INSERT INTO Words (English, Vietnamese, Example, PartOfSpeech, Ipa, ImagePath)
                VALUES ($eng, $viet, $ex, $pos, $ipa, '')";
            insertCmd.Parameters.AddWithValue("$eng", eng);
            insertCmd.Parameters.AddWithValue("$ipa", ipa);
            insertCmd.Parameters.AddWithValue("$viet", viet);
            insertCmd.Parameters.AddWithValue("$ex", ex);
            insertCmd.Parameters.AddWithValue("$pos", pos);
            insertCmd.ExecuteNonQuery();
        }
    }

    public void ImportNaturalTextFile(string filePath)
    {
        if (!File.Exists(filePath)) return;

        var lines = File.ReadAllLines(filePath);

        string pattern = @"^(?<eng>[A-Za-z\s-]+)\s*\((?<pos>[A-Za-z]+)\)\s*:\s*(?<viet>.+)$";
        var regex = new Regex(pattern);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var match = regex.Match(line.Trim());

            if (match.Success)
            {
                string eng = match.Groups["eng"].Value.Trim();
                string pos = match.Groups["pos"].Value.Trim();
                string viet = match.Groups["viet"].Value.Trim();
                string ex = "";

                var insertCmd = _connection.CreateCommand();
                insertCmd.CommandText = @"
                    INSERT INTO Words (English, Vietnamese, Example, PartOfSpeech, Ipa, ImagePath)
                    VALUES ($eng, $viet, $ex, $pos, '', '')";
                insertCmd.Parameters.AddWithValue("$eng", eng);
                insertCmd.Parameters.AddWithValue("$viet", viet);
                insertCmd.Parameters.AddWithValue("$ex", ex);
                insertCmd.Parameters.AddWithValue("$pos", pos);
                insertCmd.ExecuteNonQuery();
            }
        }
    }

    // ── CRUD Words ──────────────────────────────────────────────────

    public List<Word> GetAllWords()
    {
        var words = new List<Word>();
        var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Words ORDER BY English";
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) words.Add(MapWord(reader));
        return words;
    }

    public List<Word> GetDueWords()
    {
        var words = new List<Word>();
        var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Words WHERE date(NextReview) <= date('now') ORDER BY NextReview";
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) words.Add(MapWord(reader));
        return words;
    }

    public Word? GetWordById(int id)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Words WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? MapWord(reader) : null;
    }

    public void AddWord(string english, string ipa, string partOfSpeech, string vietnamese, string example)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Words (English, Vietnamese, Example, PartOfSpeech, Ipa, ImagePath)
            VALUES ($eng, $viet, $ex, $pos, $ipa, $img)";
        cmd.Parameters.AddWithValue("$eng", english);
        cmd.Parameters.AddWithValue("$viet", vietnamese);
        cmd.Parameters.AddWithValue("$ex", example ?? "");
        cmd.Parameters.AddWithValue("$pos", partOfSpeech ?? "");
        cmd.Parameters.AddWithValue("$ipa", ipa ?? "");
        cmd.Parameters.AddWithValue("$img", "");
        cmd.ExecuteNonQuery();
    }

    public void UpdateWord(int id, string english, string ipa, string partOfSpeech, string vietnamese, string example)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            UPDATE Words SET
                English      = $eng,
                Vietnamese   = $viet,
                Example      = $ex,
                PartOfSpeech = $pos,
                Ipa          = $ipa
            WHERE Id = $id";
        cmd.Parameters.AddWithValue("$eng", english);
        cmd.Parameters.AddWithValue("$viet", vietnamese);
        cmd.Parameters.AddWithValue("$ex", example ?? "");
        cmd.Parameters.AddWithValue("$pos", partOfSpeech ?? "");
        cmd.Parameters.AddWithValue("$ipa", ipa ?? "");
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public void UpdateWord(Word word)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            UPDATE Words SET
                English        = $eng,
                Vietnamese     = $viet,
                Example        = $ex,
                PartOfSpeech   = $pos,
                Ipa            = $ipa,
                ImagePath      = $img,
                Repetition     = $rep,
                EaseFactor     = $ef,
                Interval       = $interval,
                NextReview     = $next,
                TotalReviews   = $total,
                CorrectReviews = $correct
            WHERE Id = $id";
        cmd.Parameters.AddWithValue("$eng", word.English);
        cmd.Parameters.AddWithValue("$viet", word.Vietnamese);
        cmd.Parameters.AddWithValue("$ex", word.Example ?? "");
        cmd.Parameters.AddWithValue("$pos", word.PartOfSpeech ?? "");
        cmd.Parameters.AddWithValue("$ipa", word.Ipa ?? "");
        cmd.Parameters.AddWithValue("$img", word.ImagePath ?? "");
        cmd.Parameters.AddWithValue("$rep", word.Repetition);
        cmd.Parameters.AddWithValue("$ef", word.EaseFactor);
        cmd.Parameters.AddWithValue("$interval", word.Interval);
        cmd.Parameters.AddWithValue("$next", word.NextReview.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$total", word.TotalReviews);
        cmd.Parameters.AddWithValue("$correct", word.CorrectReviews);
        cmd.Parameters.AddWithValue("$id", word.Id);
        cmd.ExecuteNonQuery();
    }

    public void DeleteWord(int id)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Words WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public void LogReview(int wordId, int quality)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT INTO ReviewLog (WordId, Quality) VALUES ($wid, $q)";
        cmd.Parameters.AddWithValue("$wid", wordId);
        cmd.Parameters.AddWithValue("$q", quality);
        cmd.ExecuteNonQuery();
    }

    public (int total, int mastered, int learning, int newWords) GetStats()
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            SELECT
                COUNT(*) as total,
                SUM(CASE WHEN Repetition >= 5 THEN 1 ELSE 0 END) as mastered,
                SUM(CASE WHEN Repetition > 0 AND Repetition < 5 THEN 1 ELSE 0 END) as learning,
                SUM(CASE WHEN Repetition = 0 THEN 1 ELSE 0 END) as newWords
            FROM Words";
        using var reader = cmd.ExecuteReader();
        reader.Read();
        return (
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.GetInt32(2),
            reader.GetInt32(3)
        );
    }

    private static Word MapWord(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0),
        English = r.GetString(1),
        Vietnamese = r.GetString(2),
        Example = r.IsDBNull(3) ? null : r.GetString(3),
        PartOfSpeech = r.IsDBNull(4) ? null : r.GetString(4),
        Repetition = r.GetInt32(5),
        EaseFactor = r.GetDouble(6),
        Interval = r.GetInt32(7),
        NextReview = DateTime.Parse(r.GetString(8)),
        CreatedAt = DateTime.Parse(r.GetString(9)),
        TotalReviews = r.GetInt32(10),
        CorrectReviews = r.GetInt32(11),
        Ipa = r.IsDBNull(12) ? null : (r.GetString(12) == "" ? null : r.GetString(12)),
        ImagePath = r.IsDBNull(13) ? null : (r.GetString(13) == "" ? null : r.GetString(13))
    };

    public void Dispose() => _connection.Dispose();
}