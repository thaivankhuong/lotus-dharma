# PLAN CRAWL DỮ LIỆU BẢN ĐỒ TỪ SAPNHAP.BANDO.COM.VN

## 🎯 MỤC TIÊU
Xây dựng tool crawl dữ liệu để tạo chức năng tương tự như https://sapnhap.bando.com.vn/

## 🔍 PHÂN TÍCH KỸ THUẬT

### 1. Công nghệ họ sử dụng
- **Frontend**: OpenLayers 8.0.0
- **Backend**: QGIS Server (WMS - Web Map Service)
- **Dữ liệu**: Load qua WMS từ file `.qgz` (QGIS Project)
- **API Endpoint**: `https://sapnhap.bando.com.vn/` + service endpoint

### 2. Cách họ load data
```javascript
// Từ file s4.3.js, họ sử dụng:
var u1 = s_iservice + "SERVICE=WMS&VERSION=1.3.0&DPI=92&REQUEST=GetCapabilities&MAP=" + o_map.nguon;

// Nguồn data:
const fa1 = 'd:/qgisserver/bando34tinh/tenrgxa8.qgz';

// WMS Layers:
- Ranh giới tỉnh/thành phố mới (34 đơn vị)
- Ranh giới tỉnh/thành phố cũ (63 đơn vị)
- Tên và nhãn địa danh
- Lớp nền ảnh vệ tinh (tham khảo)
```

### 3. API Endpoints quan trọng
```
1. GetCapabilities: Lấy thông tin các lớp bản đồ
   URL: [service]/SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities&MAP=[file.qgz]

2. GetMap: Lấy hình ảnh bản đồ (tile)
   URL: [service]/SERVICE=WMS&VERSION=1.3.0&REQUEST=GetMap&LAYERS=[layer_name]&...

3. GetFeatureInfo: Lấy thông tin chi tiết khi click vào feature
   URL: [service]/SERVICE=WMS&VERSION=1.3.0&REQUEST=GetFeatureInfo&...
```

## 📊 DỮ LIỆU CẦN THIẾT

### A. Dữ liệu GeoJSON (Ưu tiên cao nhất)
1. **Ranh giới tỉnh/thành phố MỚI** (34 đơn vị)
   - File: `provinces_new_34.geojson`
   - Thuộc tính:
     - `id`: Mã tỉnh
     - `name`: Tên tỉnh mới
     - `old_provinces`: Danh sách tỉnh cũ sáp nhập
     - `area`: Diện tích (km²)
     - `population`: Dân số
     - `capital`: Tên thủ phủ
     - `geometry`: Tọa độ ranh giới (Polygon/MultiPolygon)

2. **Ranh giới tỉnh/thành phố CŨ** (63 đơn vị)
   - File: `provinces_old_63.geojson`
   - Thuộc tính tương tự

3. **Ranh giới cấp xã MỚI** (3321 đơn vị)
   - File: `communes_new.geojson`
   - Thuộc tính:
     - `id`: Mã xã/phường
     - `name`: Tên xã/phường mới
     - `province_id`: Thuộc tỉnh nào
     - `district_id`: Thuộc huyện nào
     - `old_commune`: Xã cũ tương ứng
     - `merge_info`: Thông tin sáp nhập
     - `geometry`: Tọa độ ranh giới

4. **Ranh giới cấp xã CŨ**
   - File: `communes_old.geojson`

### B. Dữ liệu thông tin (JSON)
1. **Thông tin sáp nhập tỉnh**
   ```json
   {
     "mergers": [
       {
         "new_province": "Thủ đô Hà Nội",
         "old_provinces": ["Hà Nội", "Hà Tây", "..."],
         "effective_date": "2025-01-01",
         "area_old": 3344.7,
         "area_new": 5000.0,
         "population_old": 8000000,
         "population_new": 9000000
       }
     ]
   }
   ```

2. **Thông tin sáp nhập xã**
   ```json
   {
     "commune_mergers": [
       {
         "new_commune": "Xã ABC",
         "old_communes": ["Xã A", "Xã B"],
         "province": "Tỉnh XYZ",
         "district": "Huyện DEF"
       }
     ]
   }
   ```

### C. Dữ liệu bổ sung
- Tọa độ trụ sở hành chính mới (Point coordinates)
- Thông tin thống kê (diện tích, dân số, GDP, ...)
- Hình ảnh/logo tỉnh thành

## 🛠️ PHƯƠNG PHÁP CRAWL

### Phương án 1: Sử dụng WMS GetFeatureInfo (Khuyến nghị)
**Ưu điểm**: Lấy được dữ liệu vector chính xác từ QGIS Server

**Các bước thực hiện**:

1. **Tìm service endpoint**
   ```javascript
   // Từ browser console trên trang sapnhap.bando.com.vn
   console.log(s_iservice); // In ra URL service
   ```

2. **Lấy danh sách layers**
   ```bash
   curl "https://[service]/SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities&MAP=d:/qgisserver/bando34tinh/tenrgxa8.qgz"
   ```

3. **Lấy thông tin features**
   - Sử dụng WFS (Web Feature Service) nếu server hỗ trợ
   - Hoặc dùng GetFeatureInfo để lấy từng feature

4. **Convert sang GeoJSON**
   - Parse XML response
   - Convert sang GeoJSON format

### Phương án 2: Reverse Engineering từ Browser
**Ưu điểm**: Không cần API, lấy data trực tiếp từ browser

**Tool**: Python + Selenium/Playwright

**Các bước**:

```python
# Pseudo code
from selenium import webdriver
from selenium.webdriver.common.by import By
import json

# 1. Mở trang web
driver = webdriver.Chrome()
driver.get("https://sapnhap.bando.com.vn/")

# 2. Chờ map load xong
time.sleep(5)

# 3. Lấy data từ JavaScript variables
provinces_data = driver.execute_script("""
    // Thử lấy features từ map
    const layers = map.getLayers().getArray();
    const features = [];
    
    layers.forEach(layer => {
        if (layer.getSource && layer.getSource().getFeatures) {
            const layerFeatures = layer.getSource().getFeatures();
            layerFeatures.forEach(f => {
                features.push({
                    properties: f.getProperties(),
                    geometry: f.getGeometry().getCoordinates()
                });
            });
        }
    });
    
    return JSON.stringify(features);
""")

# 4. Lưu vào file
with open('provinces.json', 'w', encoding='utf-8') as f:
    f.write(provinces_data)
```

### Phương án 3: Click từng tỉnh và lấy thông tin
**Ưu điểm**: Lấy được thông tin chi tiết khi click

**Các bước**:

```python
# 1. Lấy danh sách tất cả tỉnh từ sidebar
provinces = driver.find_elements(By.CSS_SELECTOR, ".province-item")

all_data = []

# 2. Click từng tỉnh
for province in provinces:
    province.click()
    time.sleep(1)
    
    # 3. Lấy thông tin hiển thị
    info = driver.execute_script("""
        return {
            name: document.querySelector('.province-name').textContent,
            area: document.querySelector('.province-area').textContent,
            // ... các thông tin khác
        }
    """)
    
    # 4. Lấy geometry từ map
    geometry = driver.execute_script("""
        if (choosedFeature) {
            const geom = choosedFeature.getGeometry();
            return geom.getCoordinates();
        }
    """)
    
    all_data.append({
        'info': info,
        'geometry': geometry
    })

# 5. Convert sang GeoJSON
geojson = {
    "type": "FeatureCollection",
    "features": [
        {
            "type": "Feature",
            "properties": item['info'],
            "geometry": {
                "type": "Polygon",
                "coordinates": item['geometry']
            }
        }
        for item in all_data
    ]
}
```

### Phương án 4: Tìm nguồn data gốc (Khó nhưng tốt nhất)
**Nguồn có thể**:
1. Tổng cục Thống kê (GSO)
2. Bộ Nội vụ
3. Cục Đo đạc và Bản đồ Việt Nam
4. OpenStreetMap Vietnam
5. GADM (Global Administrative Areas)

**Cách tiếp cận**:
- Tìm kiếm "bản đồ hành chính Việt Nam 2025 GeoJSON"
- Liên hệ các cơ quan chính phủ
- Sử dụng data từ OSM và tự cập nhật

## 📝 IMPLEMENTATION PLAN

### Phase 1: Nghiên cứu và thử nghiệm (1-2 ngày)
- [ ] Mở Developer Tools, tab Network
- [ ] Click vào các tỉnh và quan sát network requests
- [ ] Tìm service endpoint (s_iservice)
- [ ] Test WMS GetCapabilities request
- [ ] Kiểm tra xem có WFS không

### Phase 2: Xây dựng tool crawl (2-3 ngày)
- [ ] Chọn phương án crawl phù hợp
- [ ] Viết script Python/Node.js
- [ ] Test với 1-2 tỉnh trước
- [ ] Crawl toàn bộ 34 tỉnh mới
- [ ] Crawl 63 tỉnh cũ (nếu cần)

### Phase 3: Xử lý và chuẩn hóa data (1-2 ngày)
- [ ] Convert sang GeoJSON chuẩn
- [ ] Validate geometry (kiểm tra polygon hợp lệ)
- [ ] Chuẩn hóa tên tỉnh, thuộc tính
- [ ] Tạo file metadata

### Phase 4: Tích hợp vào project (1 ngày)
- [ ] Import GeoJSON vào project
- [ ] Update VietnamMap component
- [ ] Test hiển thị
- [ ] Optimize performance

## 🚀 QUICK START

### Bước 1: Kiểm tra Network Requests
```
1. Mở https://sapnhap.bando.com.vn/
2. Mở Developer Tools (F12)
3. Tab Network > Filter: XHR
4. Click vào một tỉnh
5. Tìm request có response là JSON/GeoJSON
6. Copy URL và response
```

### Bước 2: Thử lấy data bằng JavaScript
```javascript
// Chạy trong browser console
const features = [];
const layers = map.getLayers().getArray();

layers.forEach((layer, i) => {
    console.log(`Layer ${i}:`, layer.constructor.name);
    
    if (layer.getSource) {
        const source = layer.getSource();
        if (source.getFeatures) {
            const layerFeatures = source.getFeatures();
            console.log(`  Features count: ${layerFeatures.length}`);
            
            layerFeatures.forEach(f => {
                const props = f.getProperties();
                const geom = f.getGeometry();
                
                features.push({
                    type: 'Feature',
                    properties: props,
                    geometry: {
                        type: geom.getType(),
                        coordinates: geom.getCoordinates()
                    }
                });
            });
        }
    }
});

// Download as JSON
const dataStr = JSON.stringify({
    type: 'FeatureCollection',
    features: features
}, null, 2);

const blob = new Blob([dataStr], {type: 'application/json'});
const url = URL.createObjectURL(blob);
const a = document.createElement('a');
a.href = url;
a.download = 'vietnam_provinces.geojson';
a.click();
```

### Bước 3: Tạo Python script (nếu cần)
```python
# install: pip install selenium beautifulsoup4 requests
from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
import json
import time

def crawl_vietnam_map():
    driver = webdriver.Chrome()
    driver.get("https://sapnhap.bando.com.vn/")
    
    # Chờ map load
    WebDriverWait(driver, 10).until(
        EC.presence_of_element_located((By.ID, "s_bando"))
    )
    
    time.sleep(3)
    
    # Lấy data từ map
    geojson = driver.execute_script("""
        // Code JavaScript ở trên
        const features = [];
        // ... (copy code từ trên)
        return JSON.stringify({type: 'FeatureCollection', features: features});
    """)
    
    # Lưu file
    with open('vietnam_provinces.geojson', 'w', encoding='utf-8') as f:
        f.write(geojson)
    
    driver.quit()
    print("Done! Check vietnam_provinces.geojson")

if __name__ == "__main__":
    crawl_vietnam_map()
```

## ⚠️ LƯU Ý

1. **Bản quyền**: Kiểm tra điều khoản sử dụng của website trước khi crawl
2. **Rate limiting**: Không crawl quá nhanh, có thể bị chặn IP
3. **Data validation**: Luôn validate geometry sau khi crawl
4. **Backup**: Lưu nhiều bản backup của data
5. **Attribution**: Ghi rõ nguồn data nếu sử dụng

## 🎓 TÀI LIỆU THAM KHẢO

- [OpenLayers WMS Documentation](https://openlayers.org/en/latest/apidoc/module-ol_source_TileWMS.html)
- [QGIS Server WMS](https://docs.qgis.org/3.28/en/docs/server_manual/services.html#wms)
- [GeoJSON Specification](https://geojson.org/)
- [Selenium Python Docs](https://selenium-python.readthedocs.io/)

## 📞 NEXT STEPS

Bạn muốn tôi:
1. ✅ Thử chạy JavaScript trong browser để lấy data ngay?
2. ✅ Viết Python script hoàn chỉnh?
3. ✅ Tìm nguồn data thay thế (OSM, GADM)?
4. ✅ Hướng dẫn chi tiết từng bước?

Hãy cho tôi biết bạn muốn bắt đầu từ đâu!
