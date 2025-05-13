namespace ETS2_FerryAssist.Core.Models
{
    /// <summary>
    /// フェリールート情報を表すモデルクラス。
    /// データベースの1レコードに対応し、各種情報を保持する。
    /// </summary>
    public class Route
    {
        /// <summary>
        /// ルートの一意な識別子（主キー）。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 出発地エリア名（例: 港のある都市名など）。
        /// </summary>
        public string DepartureArea { get; set; } = string.Empty;

        /// <summary>
        /// 到着地エリア名。
        /// </summary>
        public string ArrivalArea { get; set; } = string.Empty;

        /// <summary>
        /// 出発港の名称。
        /// </summary>
        public string BoardingPort { get; set; } = string.Empty;

        /// <summary>
        /// 到着港の名称。
        /// </summary>
        public string LandingPort { get; set; } = string.Empty;
    }
}
