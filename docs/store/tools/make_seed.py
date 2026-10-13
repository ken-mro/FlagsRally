"""Build demo databases (en / ja) for store screenshots from a copy of an app database."""
import shutil, sqlite3, sys
from datetime import datetime

src, out_dir = sys.argv[1], sys.argv[2]

def ticks(y, m, d, h=12):
    return int((datetime(y, m, d, h) - datetime(1, 1, 1)).total_seconds() * 10**7)

# date, code, admin code, (en country, en admin, en locality), (ja country, ja admin, ja locality), lat, lon
ARRIVALS = [
    ((2026, 10, 9), "JP", "13", ("Japan", "Tokyo", "Chiyoda City"), ("日本", "東京都", "千代田区"), 35.6812, 139.7671),
    ((2026, 9, 20), "JP", "26", ("Japan", "Kyoto", "Kyoto"), ("日本", "京都府", "京都市"), 35.0116, 135.7681),
    ((2026, 8, 14), "JP", "01", ("Japan", "Hokkaido", "Sapporo"), ("日本", "北海道", "札幌市"), 43.0618, 141.3545),
    ((2026, 7, 3), "JP", "47", ("Japan", "Okinawa", "Naha"), ("日本", "沖縄県", "那覇市"), 26.2124, 127.6809),
    ((2026, 5, 22), "FR", "IDF", ("France", "Île-de-France", "Paris"), ("フランス", "イル＝ド＝フランス", "パリ"), 48.8566, 2.3522),
    ((2026, 5, 18), "IT", "62", ("Italy", "Lazio", "Rome"), ("イタリア", "ラツィオ州", "ローマ"), 41.9028, 12.4964),
    ((2026, 4, 2), "ES", "CT", ("Spain", "Catalonia", "Barcelona"), ("スペイン", "カタルーニャ州", "バルセロナ"), 41.3874, 2.1686),
    ((2026, 3, 11), "US", "CA", ("United States", "California", "San Francisco"), ("アメリカ合衆国", "カリフォルニア州", "サンフランシスコ"), 37.7749, -122.4194),
    ((2026, 3, 8), "US", "HI", ("United States", "Hawaii", "Honolulu"), ("アメリカ合衆国", "ハワイ州", "ホノルル"), 21.3069, -157.8583),
    ((2025, 12, 28), "CA", "BC", ("Canada", "British Columbia", "Vancouver"), ("カナダ", "ブリティッシュコロンビア州", "バンクーバー"), 49.2827, -123.1207),
    ((2025, 11, 15), "DE", "BY", ("Germany", "Bavaria", "Munich"), ("ドイツ", "バイエルン州", "ミュンヘン"), 48.1351, 11.5820),
    ((2025, 8, 9), "TH", "", ("Thailand", "Bangkok", "Bangkok"), ("タイ", "バンコク", "バンコク"), 13.7563, 100.5018),
    ((2025, 6, 1), "AU", "", ("Australia", "New South Wales", "Sydney"), ("オーストラリア", "ニューサウスウェールズ州", "シドニー"), -33.8688, 151.2093),
    ((2025, 2, 14), "GB", "", ("United Kingdom", "England", "London"), ("イギリス", "イングランド", "ロンドン"), 51.5072, -0.1276),
    ((2025, 1, 5), "KR", "", ("South Korea", "Seoul", "Seoul"), ("大韓民国", "ソウル特別市", "ソウル"), 37.5665, 126.9780),
    ((2024, 10, 20), "TW", "", ("Taiwan", "Taipei", "Taipei"), ("台湾", "台北市", "台北"), 25.0330, 121.5654),
]

JINGU = "全国神宮二十五社巡り"
CHECK_INS = {
    "ise": (2026, 10, 10), "meiji": (2026, 10, 9), "heian": (2026, 9, 20), "shiramine": (2026, 9, 20),
    "atsuta": (2026, 9, 2), "kashihara": (2026, 6, 14), "hokkaido": (2026, 8, 14),
    "kashima": (2026, 4, 29), "katori": (2026, 4, 29),
}

for lang in ("en", "ja"):
    dst = f"{out_dir}/seed_{lang}.db3"
    shutil.copy(src, dst)
    db = sqlite3.connect(dst)
    db.execute("DELETE FROM ArrivalLocation")
    for date, cc, ac, en, ja, lat, lon in reversed(ARRIVALS):  # the app lists the latest insert first
        local = en if lang == "en" else ja
        db.execute(
            "INSERT INTO ArrivalLocation (ArrivalDate, CountryName, AdminAreaName, LocalityName, EnCountryName, EnAdminAreaName, EnLocalityName, CountryCode, AdminAreaCode, Latitude, Longitude) VALUES (?,?,?,?,?,?,?,?,?,?,?)",
            (ticks(*date), *local, *en, cc, ac, lat, lon))
    db.execute("UPDATE CustomLocation SET ArrivalDate = NULL")
    for code, date in CHECK_INS.items():
        db.execute("UPDATE CustomLocation SET ArrivalDate = ? WHERE BoardName = ? AND Code = ?", (ticks(*date), JINGU, code))
    db.commit()
    print(lang, db.execute("select count(*) from ArrivalLocation").fetchone(), db.execute("select count(*) from CustomLocation where ArrivalDate is not null").fetchone(), db.execute("select Name from CustomBoard").fetchall())
    db.close()
