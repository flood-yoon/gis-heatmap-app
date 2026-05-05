using GMap.NET;
using GMap.NET.MapProviders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SORI_PRo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                // 기본 프로바이더 설정 (GoogleMap)
                gMapControl1.MapProvider = GMapProviders.GoogleMap;

                // 서울 중심 좌표
                gMapControl1.Position = new PointLatLng(37.5665, 126.9780);

                // 줌 설정
                gMapControl1.MinZoom = 2;
                gMapControl1.MaxZoom = 18;
                gMapControl1.Zoom = 12;

                // 마우스 조작 설정
                gMapControl1.CanDragMap = true; // 드래그 가능
                gMapControl1.DragButton = MouseButtons.Left;
                gMapControl1.MouseWheelZoomEnabled = true; // 휠로 확대/축소
                gMapControl1.MouseWheelZoomType = MouseWheelZoomType.MousePositionAndCenter;

                // 기타 보기 옵션
                gMapControl1.ShowCenter = false; // 십자 중앙 표시 비활성화
                gMapControl1.Bearing = 0;        // 회전 없음
                gMapControl1.GrayScaleMode = false;

                // 캐시 모드 설정
                GMaps.Instance.Mode = AccessMode.ServerOnly; // 인터넷에서 불러오기
            }
            catch (Exception ex)
            {
                MessageBox.Show("지도 초기화 중 오류 발생: " + ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Form2 인스턴스 생성
            Form2 form2 = new Form2();

            // 새 창으로 열기 (비모달 방식 — 두 창을 동시에 조작 가능)
            form2.Show();

            // 만약 Form1을 잠시 멈추고 Form2만 띄우고 싶다면 아래 방식 사용
            // form2.ShowDialog();
        }
    }
    
}
