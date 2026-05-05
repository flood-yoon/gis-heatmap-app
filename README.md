# GIS Heatmap Application

---

# 🇰🇷 Korean

## 📌 프로젝트 목적 및 참고사항

본 프로젝트는 관광지 데이터의 지오코딩 처리와, 해당 데이터를 Windows Forms 기반 지도 UI에 연동하여 시각화하는 과정을 구현하기 위한 프로토타입입니다.

특히, 공간 데이터를 실제 애플리케이션 환경에서 어떻게 연결하고 표현할 수 있는지에 초점을 맞추어 개발되었습니다.

샘플 데이터의 양과 품질에 따라 기능 확장을 고려하여 다양한 UI 요소(버튼 및 옵션)를 사전에 설계하였으며,  
현재는 데이터 확보의 한계로 인해 일부 기능은 추가 구현 단계에 있습니다.

본 프로젝트는 완성형 서비스가 아닌, 공간 데이터 처리 및 시각화 흐름을 검증하기 위한 개발 과정 중심의 결과물입니다.

---

## 🚀 주요 기능
- 지도 기반 시각화
- 관광지 위치 마커 표시
- 매출 데이터 기반 히트맵 생성

---

## ▶️ 실행 방법
1. 프로그램 실행
<img src="images/Form1.png" width="600"/>
2. View-Heat-Map을 클릭하면 두번째 폼으로 넘어갑니다
<img src="images/Form2.png" width="600"/>
3. location을 선택해 지오코딩 된 관광지 포인트 데이터 위치를 지도상에서 확인할 수 있습니다
<img src="images/location.png" width="600"/>
4. Sale그룹 박스에서는 히트맵을 볼 수 있습니다 ex)Sale_AMT
<img src="images/sale_amount.png" width="600"/>


---

## 🛠 기술 스택
- C#
- Windows Forms
- GMap.NET

---

## 📂 데이터 설명
- `sample.csv`: 서울 관광지 매출 데이터
- 위치 정보와 매출 값을 기반으로 히트맵 생성

---

# 🇺🇸 English

## 📌 Project Purpose & Notes

This project is a prototype designed to implement the workflow of geocoding tourist data and integrating it into a map-based Windows Forms application.

The primary focus is on how spatial data can be processed, connected, and visualized within an application environment.

The UI components (buttons and options) were designed with scalability in mind, allowing for future feature expansion depending on data availability and quality.

Due to limitations in acquiring sufficient data, some features remain partially implemented.

This project should be considered a development-oriented prototype aimed at validating spatial data processing and visualization workflows, rather than a fully completed application.

---

## 🚀 Features
- Map-based visualization  
- Marker display for tourist locations  
- Heatmap generation based on sales data  

---

## ▶️ How to Run
1. Run the application  
<img src="images/Form1.png" width="600"/>

2. Click View Heat Map to open the second form  
<img src="images/Form2.png" width="600"/>

3. Select a location to display geocoded tourist point data on the map  
<img src="images/location.png" width="600"/>

4. In the **Sale** group box, you can visualize the heatmap (e.g., `Sale_AMT`)  
<img src="images/sale_amount.png" width="600"/>

---

## 🛠 Tech Stack
- C#  
- Windows Forms  
- GMap.NET  

---

## 📂 Data
- `sample.csv`: Sales data of tourist locations in Seoul  
- Used to generate heatmap based on location and sales values  
