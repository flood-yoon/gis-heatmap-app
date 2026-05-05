using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SORI_PRo
{
    public partial class Form2 : Form
    {
        // ── 오버레이 ──────────────────────────────────────────────
        private GMapOverlay markerOverlay;
        private GMapOverlay heatOverlay;      // checkBox2 : 밀도
        private GMapOverlay saleHeatOverlay;  // checkBox6 : 매출액
        private GMapOverlay custHeatOverlay;  // checkBox5 : 고객수
        private GMapOverlay saleCntHeatOverlay; // checkBox4 : 매출건수

        // ── 데이터 ───────────────────────────────────────────────
        private List<(string name, double lat, double lng)> points = new List<(string, double, double)>();
        private List<(double lat, double lng)> coords = new List<(double, double)>();
        private List<double> weights = new List<double>(); // 밀도용 (매출액 기반)
        private List<double> weightsSale = new List<double>(); // 매출액
        private List<double> weightsCust = new List<double>(); // 고객수
        private List<double> weightsSaleCnt = new List<double>(); // 매출건수

        private bool dataReady = false;

        // ── 표시 상태 ────────────────────────────────────────────
        private bool showHeatmap = false;
        private bool showSaleHeatmap = false;
        private bool showCustHeatmap = false;
        private bool showSaleCntHeatmap = false;

        // ── 비동기 취소 토큰 ─────────────────────────────────────
        private Dictionary<string, CancellationTokenSource> _ctsList = new Dictionary<string, CancellationTokenSource>();

        public Form2() { InitializeComponent(); }

        // ═══════════════════════════════════════════════════════
        // 초기화
        // ═══════════════════════════════════════════════════════
        private void Form2_Load(object sender, EventArgs e)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            gMapControl1.MapProvider = GMapProviders.GoogleMap;
            gMapControl1.Position = new PointLatLng(37.5665, 126.9780);
            gMapControl1.MinZoom = 2;
            gMapControl1.MaxZoom = 21;
            gMapControl1.Zoom = 12;
            gMapControl1.ShowCenter = false;
            gMapControl1.CanDragMap = true;
            gMapControl1.DragButton = MouseButtons.Left;
            gMapControl1.MouseWheelZoomEnabled = false;
            gMapControl1.MouseWheel += GMap_MouseWheel;

            markerOverlay = new GMapOverlay("markers");
            heatOverlay = new GMapOverlay("heatmap");
            saleHeatOverlay = new GMapOverlay("saleHeatmap");
            custHeatOverlay = new GMapOverlay("custHeatmap");
            saleCntHeatOverlay = new GMapOverlay("saleCntHeatmap");
            gMapControl1.Overlays.Add(markerOverlay);

            gMapControl1.OnMapZoomChanged += () => RedrawAll();
            gMapControl1.OnMapDrag += () => RedrawAll();

            string path = Path.Combine(Application.StartupPath, "data", "sample.csv");
            LoadAndPrepareData(path);
        }

        private void GMap_MouseWheel(object sender, MouseEventArgs e)
        {
            double step = 0.5;
            gMapControl1.Zoom = e.Delta > 0
                ? Math.Min(gMapControl1.Zoom + step, gMapControl1.MaxZoom)
                : Math.Max(gMapControl1.Zoom - step, gMapControl1.MinZoom);
        }

        private void RedrawAll()
        {
            if (showHeatmap) RedrawHeatmap(heatOverlay, weights, HeatColor, "heat");
            if (showSaleHeatmap) RedrawHeatmap(saleHeatOverlay, weightsSale, SaleHeatColor, "sale");
            if (showCustHeatmap) RedrawHeatmap(custHeatOverlay, weightsCust, CustHeatColor, "cust");
            if (showSaleCntHeatmap) RedrawHeatmap(saleCntHeatOverlay, weightsSaleCnt, SaleCntHeatColor, "salecnt");
        }

        // ═══════════════════════════════════════════════════════
        // 데이터 로드 & 정규화
        // ═══════════════════════════════════════════════════════
        private void LoadAndPrepareData(string filePath)
        {
            if (!File.Exists(filePath)) { MessageBox.Show("CSV 파일을 찾을 수 없습니다: " + filePath); return; }

            try
            {
                string[] lines = File.ReadAllLines(filePath);
                string[] headers = lines[0].Split(',');

                int nameIdx = Array.IndexOf(headers, "서울관광지_명(SLTA_NM)");
                int latIdx = Array.IndexOf(headers, "위도");
                int lngIdx = Array.IndexOf(headers, "경도");
                int saleIdx = Array.IndexOf(headers, "매출_액(SALE_AMT)");
                int custIdx = Array.IndexOf(headers, "매출_고객_수(SALE_CUST_CNT)");
                int saleCntIdx = Array.IndexOf(headers, "매출_건수(SALE_CNT)");

                if (new[] { nameIdx, latIdx, lngIdx, saleIdx, custIdx, saleCntIdx }.Any(i => i == -1))
                {
                    MessageBox.Show("CSV 열 이름을 확인하세요."); return;
                }

                var rawSale = new List<double>();
                var rawCust = new List<double>();
                var rawSaleCnt = new List<double>();

                for (int i = 1; i < lines.Length; i++)
                {
                    string[] v = lines[i].Split(',');
                    double lat, lng, sale, cust, saleCnt;

                    if (!double.TryParse(v[latIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out lat) ||
                        !double.TryParse(v[lngIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out lng) ||
                        !double.TryParse(v[saleIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out sale))
                        continue;

                    double.TryParse(v[custIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out cust);
                    double.TryParse(v[saleCntIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out saleCnt);

                    points.Add((v[nameIdx], lat, lng));
                    coords.Add((lat, lng));
                    rawSale.Add(sale);
                    rawCust.Add(cust);
                    rawSaleCnt.Add(saleCnt);
                }

                if (points.Count == 0) { MessageBox.Show("유효한 좌표 데이터가 없습니다."); return; }

                weights = Normalize(rawSale);
                weightsSale = Normalize(rawSale);
                weightsCust = Normalize(rawCust);
                weightsSaleCnt = Normalize(rawSaleCnt);

                dataReady = true;
            }
            catch (Exception ex) { MessageBox.Show("데이터 로딩 중 오류: " + ex.Message); }
        }

        private List<double> Normalize(List<double> raw)
        {
            var log = raw.Select(x => Math.Log(Math.Max(1, x + 1))).ToList();
            double p05 = Percentile(log, 5);
            double p95 = Percentile(log, 95);
            if (p95 <= p05) { p05 = log.Min(); p95 = log.Max(); }
            return log.Select(v =>
            {
                double clipped = Math.Min(Math.Max(v, p05), p95);
                return (clipped - p05) / Math.Max(1e-12, p95 - p05);
            }).ToList();
        }

        private double Percentile(List<double> values, double p)
        {
            double[] arr = values.OrderBy(x => x).ToArray();
            double r = (p / 100.0) * (arr.Length - 1);
            int lo = (int)Math.Floor(r), hi = (int)Math.Ceiling(r);
            return lo == hi ? arr[lo] : arr[lo] * (1 - (r - lo)) + arr[hi] * (r - lo);
        }

        // ═══════════════════════════════════════════════════════
        // 체크박스 이벤트
        // ═══════════════════════════════════════════════════════
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!dataReady) return;
            markerOverlay.Markers.Clear();
            if (checkBox1.Checked)
                foreach (var p in points)
                {
                    var m = new GMarkerGoogle(new PointLatLng(p.lat, p.lng), GMarkerGoogleType.red_dot);
                    m.ToolTipText = p.name;
                    markerOverlay.Markers.Add(m);
                }
            gMapControl1.Refresh();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e) =>
            ToggleHeatmap(checkBox2.Checked, ref showHeatmap, heatOverlay, weights, HeatColor, "heat");

        private void checkBox6_CheckedChanged(object sender, EventArgs e) =>
            ToggleHeatmap(checkBox6.Checked, ref showSaleHeatmap, saleHeatOverlay, weightsSale, SaleHeatColor, "sale");

        private void checkBox5_CheckedChanged(object sender, EventArgs e) =>
            ToggleHeatmap(checkBox5.Checked, ref showCustHeatmap, custHeatOverlay, weightsCust, CustHeatColor, "cust");

        private void checkBox4_CheckedChanged(object sender, EventArgs e) =>
            ToggleHeatmap(checkBox4.Checked, ref showSaleCntHeatmap, saleCntHeatOverlay, weightsSaleCnt, SaleCntHeatColor, "salecnt");

        private void ToggleHeatmap(bool on, ref bool flag, GMapOverlay overlay,
            List<double> w, Func<double, Color> colorFunc, string key)
        {
            if (!dataReady) return;
            flag = on;
            if (on)
                RedrawHeatmap(overlay, w, colorFunc, key);
            else
            {
                overlay.Markers.Clear();
                gMapControl1.Overlays.Remove(overlay);
                gMapControl1.Refresh();
            }
        }

        // ═══════════════════════════════════════════════════════
        // 히트맵 렌더링 (공통 비동기)
        // ═══════════════════════════════════════════════════════
        private async void RedrawHeatmap(GMapOverlay overlay, List<double> w,
            Func<double, Color> colorFunc, string key)
        {
            if (!dataReady) return;

            if (_ctsList.ContainsKey(key)) _ctsList[key].Cancel();
            _ctsList[key] = new CancellationTokenSource();
            var token = _ctsList[key].Token;

            // 디바운싱
            await Task.Delay(80, token).ContinueWith(_ => { }, TaskContinuationOptions.None);
            if (token.IsCancellationRequested) return;

            // 메인 스레드에서 픽셀 좌표 변환
            int width = gMapControl1.Width;
            int height = gMapControl1.Height;
            var localPoints = coords
                .Select(p => gMapControl1.FromLatLngToLocal(new PointLatLng(p.lat, p.lng)))
                .Select(gp => new Point((int)gp.X, (int)gp.Y))
                .ToList();
            var wSnap = w.ToList();
            var pos = gMapControl1.Position;

            Bitmap bmp;
            try
            {
                bmp = await Task.Run(() =>
                    GenerateBitmap(localPoints, wSnap, width, height, colorFunc), token);
            }
            catch (OperationCanceledException) { return; }

            if (token.IsCancellationRequested) return;

            overlay.Markers.Clear();
            overlay.Markers.Add(new MapSizedImageMarker(pos, bmp, gMapControl1));
            if (!gMapControl1.Overlays.Contains(overlay))
                gMapControl1.Overlays.Add(overlay);
            gMapControl1.Refresh();
        }

        private Bitmap GenerateBitmap(List<Point> localPoints, List<double> w,
            int width, int height, Func<double, Color> colorFunc)
        {
            double bandwidth = 500.0; // 픽셀 반경
            double[,] density = new double[width, height];
            double maxD = 0;

            for (int i = 0; i < localPoints.Count; i++)
            {
                int cx = localPoints[i].X, cy = localPoints[i].Y;
                double weight = w[i];
                int radius = (int)bandwidth;
                int x0 = Math.Max(0, cx - radius), x1 = Math.Min(width - 1, cx + radius);
                int y0 = Math.Max(0, cy - radius), y1 = Math.Min(height - 1, cy + radius);

                for (int x = x0; x <= x1; x++)
                    for (int y = y0; y <= y1; y++)
                    {
                        double dx = (x - cx) / bandwidth, dy = (y - cy) / bandwidth;
                        double k = Math.Exp(-(dx * dx + dy * dy) / 2.0);
                        density[x, y] += k * weight;
                        if (density[x, y] > maxD) maxD = density[x, y];
                    }
            }

            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bmpData = bmp.LockBits(new Rectangle(0, 0, width, height),
                              ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                for (int y = 0; y < height; y++)
                {
                    byte* row = ptr + y * bmpData.Stride;
                    for (int x = 0; x < width; x++)
                    {
                        double v = density[x, y] / Math.Max(1e-9, maxD);
                        Color c = colorFunc(v);
                        int idx = x * 4;
                        row[idx] = c.B;
                        row[idx + 1] = c.G;
                        row[idx + 2] = c.R;
                        row[idx + 3] = c.A;
                    }
                }
            }
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        // ═══════════════════════════════════════════════════════
        // 색상 함수
        // ═══════════════════════════════════════════════════════
        private Color HeatColor(double v) // checkBox2 : 주황-빨강
        {
            double g = Math.Sqrt(Math.Max(0, Math.Min(1, v)));
            return Color.FromArgb((int)(160 * g), 255, (int)(210 * (1 - g)), 0);
        }

        private Color SaleHeatColor(double v) // checkBox6 : 파랑→초록→노랑→빨강
        {
            double g = Math.Sqrt(Math.Max(0, Math.Min(1, v)));
            int a = (int)(180 * g);
            int r, gg, b;
            if (v < 0.25) { double t = v / 0.25; r = 0; gg = (int)(255 * t); b = 255; }
            else if (v < 0.50) { double t = (v - 0.25) / 0.25; r = 0; gg = 255; b = (int)(255 * (1 - t)); }
            else if (v < 0.75) { double t = (v - 0.50) / 0.25; r = (int)(255 * t); gg = 255; b = 0; }
            else { double t = (v - 0.75) / 0.25; r = 255; gg = (int)(255 * (1 - t)); b = 0; }
            return Color.FromArgb(a, r, gg, b);
        }

        private Color CustHeatColor(double v) // checkBox5 : 파랑→보라
        {
            double g = Math.Sqrt(Math.Max(0, Math.Min(1, v)));
            int a = (int)(180 * g);
            int r = v < 0.5 ? (int)(128 * (v / 0.5)) : (int)(128 + 127 * ((v - 0.5) / 0.5));
            int b = v < 0.5 ? 255 : (int)(255 * (1 - (v - 0.5) / 0.5));
            return Color.FromArgb(a, r, 0, b);
        }

        private Color SaleCntHeatColor(double v) // checkBox4 : 파랑→청록
        {
            double g = Math.Sqrt(Math.Max(0, Math.Min(1, v)));
            int a = (int)(180 * g);
            int gg = v < 0.5 ? (int)(255 * (v / 0.5)) : 255;
            int b = v < 0.5 ? 255 : (int)(255 * (1 - (v - 0.5) / 0.5));
            return Color.FromArgb(a, 0, gg, b);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 전체 화면 크기 마커
    // ═══════════════════════════════════════════════════════════
    public class MapSizedImageMarker : GMapMarker
    {
        private readonly Image _img;
        private readonly GMapControl _map;

        public MapSizedImageMarker(PointLatLng p, Image img, GMapControl map) : base(p)
        {
            _img = img; _map = map;
            Size = img.Size;
            Offset = new Point(-map.Width / 2, -map.Height / 2);
            IsHitTestVisible = false;
        }

        public override void OnRender(Graphics g)
        {
            if (_img == null || _map == null) return;
            g.DrawImage(_img, Offset.X, Offset.Y, _map.Width, _map.Height);
        }
    }
}
