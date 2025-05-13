using System.Data.SQLite;
using ETS2_FerryAssist.Core.Models;

namespace ETS2_FerryAssist.Core.Database
{
    /// <summary>
    /// SQLite データベースへのアクセスを提供するヘルパークラス。
    /// 主にフェリールートの取得処理を担当。
    /// </summary>
    public class DatabaseHelper : IDisposable
    {
        // シングルトン的に使われる接続インスタンス（アプリケーション全体で共有）
        private static SQLiteConnection? _connection;

        // スレッドセーフにするためのロックオブジェクト
        private static readonly object _lock = new();

        // 取得済みルート情報のキャッシュ（同じ問い合わせの高速化）
        private static readonly Dictionary<string, Route> _cache = new();

        // Dispose 状態を管理するフラグ
        private bool _disposed;

        /// <summary>
        /// コンストラクタ：データベース接続を初期化。
        /// すでに接続がある場合は再接続しない。
        /// </summary>
        public DatabaseHelper(string dbPath)
        {
            if (_connection == null)
            {
                _connection = new SQLiteConnection($"Data Source={dbPath};BusyTimeout=5000;");
                _connection.Open(); // 接続開始
            }
        }

        /// <summary>
        /// 出発地・到着地からルート情報を取得。
        /// キャッシュが存在すればキャッシュから取得。
        /// </summary>

        public Route? GetRoute(string departureArea, string arrivalArea)
        {
            var key = $"{departureArea}->{arrivalArea}";

            // キャッシュがあれば即返す
            if (_cache.TryGetValue(key, out var cached))
                return cached;

            // データベースアクセスはロックを取得して行う（スレッドセーフ）
            lock (_lock)
            {
                try
                {
                    // SQL コマンドを準備
                    using var command = _connection!.CreateCommand();
                    command.CommandText = @"
                        SELECT id, departure_area, arrival_area, boarding_port, landing_port
                        FROM routes
                        WHERE departure_area = @departure AND arrival_area = @arrival";

                    // パラメータの追加（SQLインジェクション対策）
                    command.Parameters.AddWithValue("@departure", departureArea);
                    command.Parameters.AddWithValue("@arrival", arrivalArea);

                    // SQL 実行と結果の読み込み
                    using var reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        // レコードを Route モデルにマッピング
                        var route = new Route
                        {
                            Id = reader.GetInt32(0),
                            DepartureArea = reader.GetString(1),
                            ArrivalArea = reader.GetString(2),
                            BoardingPort = reader.GetString(3),
                            LandingPort = reader.GetString(4)
                        };

                        // キャッシュに追加
                        _cache[key] = route;
                        return route;
                    }
                }
                catch (SQLiteException ex)
                {
                    Console.WriteLine($"SQLite 読み込み失敗: {ex.Message}");
                }
            }

            return null; // 該当データが見つからなかった
        }

        /// <summary>
        /// Disposeパターン実装の本体。
        /// リソースを確実に解放する。
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // マネージドリソースの解放
                    _connection?.Dispose();
                    _connection = null;
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// IDisposable インターフェース実装。
        /// using 文での安全な解放をサポート。
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // ファイナライザを抑制
        }
    }
}
